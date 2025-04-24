using Microsoft.AspNetCore.SignalR;
using WordBattleGame.Repositories;
using WordBattleGame.Models;
using WordBattleGame.Services;
using WordBattleGame.Utils;

namespace WordBattleGame.Hubs
{
    public class GameHub(
        ILogger<GameHub> logger,
        IGameRepository gameRepository,
        IRoundRepository roundRepository,
        IWordGeneratorService wordGeneratorService,
        IPlayerRepository playerRepository,
        IHubContext<GameHub> hubContext,
        IServiceScopeFactory serviceScopeFactory
    ) : Hub
    {
        private readonly IHubContext<GameHub> _hubContext = hubContext;
        private readonly ILogger<GameHub> _logger = logger;
        private readonly IGameRepository _gameRepository = gameRepository;
        private readonly IRoundRepository _roundRepository = roundRepository;
        private readonly IPlayerRepository _playerRepository = playerRepository;
        private readonly IWordGeneratorService _wordGeneratorService = wordGeneratorService;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        private static readonly List<string> MatchMakingQueue = [];
        private static readonly Dictionary<string, HashSet<string>> GameJoinStatus = new();
        private static readonly Lock QueueLock = new();
        private static readonly Dictionary<string, CancellationTokenSource> RoundCountdownTokens = [];

        public async Task JoinMatchMaking(string playerId)
        {
            _logger.LogInformation("GameHub Context.User.Identity.IsAuthenticated: {IsAuthenticated}, Name: {Name}",
                Context.User?.Identity?.IsAuthenticated,
                Context.User?.Identity?.Name);
            _logger.LogInformation($"Player {playerId} joined matchmaking.");
            lock (QueueLock)
            {
                if (!MatchMakingQueue.Contains(playerId))
                    MatchMakingQueue.Add(playerId);
            }
            await Clients.Caller.SendAsync("MatchMakingJoined", new MatchMakingJoinedDto { PlayerId = playerId });

            string[]? matchedPlayerIds = null;
            lock (QueueLock)
            {
                _logger.LogInformation($"Current matchmaking queue: {string.Join(", ", MatchMakingQueue)}");
                if (MatchMakingQueue.Count >= 2)
                {
                    matchedPlayerIds = [.. MatchMakingQueue.Take(2)];
                    MatchMakingQueue.RemoveRange(0, 2);
                }
            }

            _logger.LogInformation($"Matched players: {string.Join(", ", matchedPlayerIds ?? [])}");

            if (matchedPlayerIds != null)
            {
                var players = _playerRepository.GetByIdsAsync(matchedPlayerIds).Result;
                _logger.LogInformation($"Matched players details: {string.Join(", ", players.Select(p => p.Name))}");
                if (players == null || players.Count == 0)
                {
                    await Clients.Caller.SendAsync("MatchMakingFailed", new MatchMakingFailedDto { Message = "No players found." });
                    return;
                }

                var game = new Game
                {
                    Id = Guid.NewGuid().ToString(),
                    MaxRound = 5,
                    CreatedAt = DateTime.UtcNow,
                    Players = players
                };

                await _gameRepository.AddAsync(game);
                var gameId = game.Id;

                foreach (var pid in matchedPlayerIds)
                {
                    _logger.LogInformation($"Sending game ID {gameId} to player {pid}.");
                    await Clients.User(pid).SendAsync("MatchFound", new MatchFoundDto
                    {
                        GameId = gameId,
                        MatchedPlayerIds = [.. matchedPlayerIds]
                    });
                }
            }
        }

        public async Task JoinGame(string gameId, string playerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            lock (GameJoinStatus)
            {
                if (!GameJoinStatus.TryGetValue(gameId, out HashSet<string>? value))
                {
                    value = [];
                    GameJoinStatus[gameId] = value;
                }

                value.Add(playerId);
            }

            var expectedPlayers = await _gameRepository.GetExpectedPlayersAsync(gameId);
            if (GameJoinStatus[gameId].Count == expectedPlayers.ToList().Count)
            {
                await _playerRepository.UpdateStatsAsync([.. expectedPlayers.Select(p => p.Id)]);
                await Clients.Group(gameId).SendAsync("AllPlayersJoined", new AllPlayersJoinedDto { GameId = gameId });
                await StartRound(gameId, 1, "en", "very very hard");
            }
        }

        public async Task LeaveMatchMaking(string playerId)
        {
            MatchMakingQueue.Remove(playerId);
            await Clients.Caller.SendAsync("MatchMakingLeft", new MatchMakingJoinedDto { PlayerId = playerId });
        }

        public async Task StartRound(string gameId, int roundNumber, string language, string difficulty)
        {
            await RoundHelper.StartRoundAsync(
                _hubContext,
                _serviceScopeFactory,
                _logger,
                gameId,
                roundNumber,
                language,
                difficulty
            );
        }

        public async Task EndRound(string roundId)
        {
            await RoundHelper.EndRoundAsync(
                _hubContext,
                _serviceScopeFactory,
                roundId,
                _logger
            );
        }

        public async Task SubmitAnswer(string roundId, string playerId, string answer)
        {
            var round = await _roundRepository.GetByIdAsync(roundId);
            if (round == null) return;

            var isCorrect = string.Equals(answer, round.TrueWord, StringComparison.OrdinalIgnoreCase);
            await Clients.Group(round.GameId).SendAsync("AnswerSubmitted", new AnswerSubmittedDto
            {
                PlayerId = playerId,
                Answer = answer,
                IsCorrect = isCorrect
            });

            if (isCorrect)
            {
                RoundHelper.CancelCountdown(roundId);
                if (round.Game.MaxRound == round.RoundNumber)
                {
                    var score = 1;
                    await _playerRepository.UpdateStatsAsync(playerId, score, true);

                    foreach (var player in round.Game.Players)
                    {
                        if (player.Id != playerId)
                        {
                            await _playerRepository.UpdateStatsAsync(player.Id, 0, false);
                        }
                    }
                    await Clients.Group(round.GameId).SendAsync("GameEnded", new GameEndedDto
                    {
                        Players = [.. round.Game.Players.Select(p => new PlayerDetailDto {
                            Id = p.Id,
                            Name = p.Name,
                            Email = p.Email
                        })]
                    });
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, round.GameId);
                }
                await Clients.User(playerId).SendAsync("CorrectAnswer", new CorrectAnswerDto { TrueWord = round.TrueWord });
                await Clients.Group(round.GameId).SendAsync("RoundEnded", new RoundEndedDto
                {
                    TrueWord = round.TrueWord,
                    WinnerPlayerId = playerId
                });
                await StartRound(round.GameId, round.RoundNumber + 1, round.Language, round.Difficulty);
            }
            else
            {
                await Clients.User(playerId).SendAsync("IncorrectAnswer", new IncorrectAnswerDto { TrueWord = round.TrueWord });
            }
        }

        public async Task SendChat(string gameId, string playerId, string message)
        {
            await Clients.Group(gameId).SendAsync("ReceiveChat", new ChatMessageDto
            {
                PlayerId = playerId,
                Message = message
            });
        }

        public async Task LeaveGame(string gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", new PlayerLeftDto { ConnectionId = Context.ConnectionId });
        }
    }
}
