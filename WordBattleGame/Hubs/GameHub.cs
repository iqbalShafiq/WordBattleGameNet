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
        IPlayerRepository playerRepository,
        IHubContext<GameHub> hubContext,
        IServiceScopeFactory serviceScopeFactory,
        IPlayerAnswerHistoryRepository playerAnswerHistoryRepository
    ) : Hub
    {
        private readonly IHubContext<GameHub> _hubContext = hubContext;
        private readonly ILogger<GameHub> _logger = logger;
        private readonly IGameRepository _gameRepository = gameRepository;
        private readonly IRoundRepository _roundRepository = roundRepository;
        private readonly IPlayerRepository _playerRepository = playerRepository;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly IPlayerAnswerHistoryRepository _playerAnswerHistoryRepository = playerAnswerHistoryRepository;

        private static readonly List<string> MatchMakingQueue = [];
        private static readonly Dictionary<string, HashSet<string>> GameJoinStatus = [];
        private static readonly Lock QueueLock = new();
        private static readonly Dictionary<string, CancellationTokenSource> JoinGameTimeouts = [];
        public static readonly Dictionary<string, string> PlayerConnections = [];

        public async Task JoinMatchMaking(string playerId)
        {
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
                    // Mulai timer 10 detik untuk join game
                    var cts = new CancellationTokenSource();
                    lock (JoinGameTimeouts)
                    {
                        JoinGameTimeouts[pid] = cts;
                    }
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation($"Waiting for player {pid} to join the game...");
                            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);

                            // Timeout: player belum JoinGame
                            MatchMakingQueue.Remove(pid);
                            _logger.LogInformation($"Player {pid} did not join the game in time.");

                            await _hubContext.Clients.User(pid).SendAsync("MatchMakingFailed", new MatchMakingFailedDto { Message = "Timeout: you did not join the game in time." });
                            _logger.LogInformation($"Player {pid} did not join the game in time 2.");
                            // Broadcast ke lawan
                            foreach (var other in matchedPlayerIds.Where(x => x != pid))
                            {
                                await _hubContext.Clients.User(other).SendAsync("MatchMakingFailed", new MatchMakingFailedDto { Message = $"Other player did not join in time." });
                                _logger.LogInformation($"Player {pid} did not join the game in time.");
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            _logger.LogInformation($"Player {pid} joined the game before timeout.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error while waiting for player {pid} to join the game: {ex.Message}");
                        }
                        finally
                        {
                            lock (JoinGameTimeouts)
                            {
                                JoinGameTimeouts.Remove(pid);
                                _logger.LogInformation($"JoinGameTimeouts removed for player {pid}.");
                            }
                        }
                    });
                }
            }
        }

        public async Task JoinGame(string gameId, string playerId)
        {
            PlayerConnections[playerId] = Context.ConnectionId;
            lock (JoinGameTimeouts)
            {
                if (JoinGameTimeouts.TryGetValue(playerId, out var cts))
                {
                    cts.Cancel();
                    JoinGameTimeouts.Remove(playerId);
                    _logger.LogInformation($"[DEBUG] JoinGameTimeouts cancelled and removed for player {playerId} in JoinGame.");
                }
            }

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

                // Send initial scores to all players
                var playerScoreDtos = expectedPlayers.Select(p => new PlayerScoreDto
                {
                    PlayerId = p.Id,
                    PlayerName = p.Name,
                    TotalScore = 0
                }).ToList();
                var gameScoreDto = new GameScoreDto
                {
                    PlayerScores = playerScoreDtos
                };
                await Clients.Group(gameId).SendAsync("GameScores", gameScoreDto);
                
                await Clients.Group(gameId).SendAsync("AllPlayersJoined", new AllPlayersJoinedDto { GameId = gameId });
                await StartRound(gameId, 1, "id", "very easy");
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
            var score = isCorrect ? round.TrueWord.Length : 0;
            await _playerAnswerHistoryRepository.InsertAsync(new PlayerAnswerHistory
            {
                PlayerId = playerId,
                GameId = round.GameId,
                RoundId = roundId,
                Word = answer,
                Score = score,
                IsCorrect = isCorrect,
                Timestamp = DateTime.UtcNow
            });

            await Clients.Group(round.GameId).SendAsync("AnswerSubmitted", new AnswerSubmittedDto
            {
                PlayerId = playerId,
                Answer = answer,
                IsCorrect = isCorrect
            });

            if (isCorrect)
            {
                RoundHelper.CancelCountdown(roundId);
                await EndRound(roundId);
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

        public async Task LeaveGame(string gameId, string playerId)
        {
            await _playerRepository.UpdateStatsAsync(playerId, 0, false);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", new PlayerLeftDto { ConnectionId = Context.ConnectionId });
        }
    }
}
