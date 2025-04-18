using Microsoft.AspNetCore.SignalR;
using WordBattleGame.Repositories;
using WordBattleGame.Models;
using WordBattleGame.Services;
using WordBattleGame.Utils;

namespace WordBattleGame.Hubs
{
    public class GameHub(
        IGameRepository gameRepository,
        IRoundRepository roundRepository,
        IWordGeneratorService wordGeneratorService,
        IPlayerRepository playerRepository
    ) : Hub
    {
        private readonly IGameRepository _gameRepository = gameRepository;
        private readonly IRoundRepository _roundRepository = roundRepository;
        private readonly IPlayerRepository _playerRepository = playerRepository;
        private readonly IWordGeneratorService _wordGeneratorService = wordGeneratorService;
        private static readonly List<string> MatchMakingQueue = [];
        private static readonly Lock QueueLock = new();

        public async Task JoinMatchMaking(string playerId)
        {
            lock (QueueLock)
            {
                if (!MatchMakingQueue.Contains(playerId))
                    MatchMakingQueue.Add(playerId);
            }
            await Clients.Caller.SendAsync("MatchMakingJoined", playerId);

            string[]? matchedPlayers = null;
            lock (QueueLock)
            {
                if (MatchMakingQueue.Count >= 2)
                {
                    matchedPlayers = [.. MatchMakingQueue.Take(2)];
                    MatchMakingQueue.RemoveRange(0, 2);
                }
            }
            if (matchedPlayers != null)
            {
                var players = _playerRepository.GetByIdsAsync(matchedPlayers).Result;
                var game = new Game
                {
                    Id = Guid.NewGuid().ToString(),
                    MaxRound = 5,
                    CreatedAt = DateTime.UtcNow,
                    Players = players
                };

                await _gameRepository.AddAsync(game);
                var gameId = game.Id;
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

                foreach (var pid in matchedPlayers)
                {
                    await Clients.User(pid).SendAsync("MatchFound", gameId, matchedPlayers);
                }
            }
        }

        public async Task LeaveMatchMaking(string playerId)
        {
            MatchMakingQueue.Remove(playerId);
            await Clients.Caller.SendAsync("MatchMakingLeft", playerId);
        }

        public async Task StartRound(string gameId, int roundNumber, string language, string difficulty)
        {
            var targetWord = await _wordGeneratorService.GenerateWordAsync(language, difficulty);
            var newGeneratedWord = WordUtils.ShuffleWord(targetWord);

            var newRound = new Round
            {
                Id = Guid.NewGuid().ToString(),
                GameId = gameId,
                RoundNumber = roundNumber,
                Difficulty = difficulty,
                GeneratedWord = newGeneratedWord,
                TrueWord = targetWord,
                Language = language,
            };

            await _roundRepository.AddAsync(newRound);
            await Clients.Group(gameId).SendAsync("RoundStarted", newGeneratedWord, targetWord, roundNumber);
        }

        public async Task EndRound(string roundId)
        {
            var round = await _roundRepository.GetByIdAsync(roundId);
            if (round == null) return;

            await Clients.Group(round.GameId).SendAsync("RoundEnded", round.TrueWord, null);

            if (round.Game.MaxRound == round.RoundNumber)
            {
                await Clients.Group(round.GameId).SendAsync("GameEnded", round.Game.Players);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, round.GameId);
            }
        }

        public async Task SubmitAnswer(string roundId, string playerId, string answer)
        {
            var round = await _roundRepository.GetByIdAsync(roundId);
            if (round == null) return;

            var isCorrect = string.Equals(answer, round.TrueWord, StringComparison.OrdinalIgnoreCase);
            await Clients.Group(round.GameId).SendAsync("AnswerSubmitted", playerId, answer, isCorrect);

            if (isCorrect)
            {
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
                    await Clients.Group(round.GameId).SendAsync("GameEnded", round.Game.Players);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, round.GameId);
                }
                await Clients.User(playerId).SendAsync("CorrectAnswer", round.TrueWord);
                await Clients.Group(round.GameId).SendAsync("RoundEnded", round.TrueWord, playerId);
            }
            else
            {
                await Clients.User(playerId).SendAsync("IncorrectAnswer", round.TrueWord);
            }
        }

        public async Task SendChat(string gameId, string playerId, string message)
        {
            await Clients.Group(gameId).SendAsync("ReceiveChat", playerId, message);
        }

        public async Task LeaveGame(string gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", Context.ConnectionId);
        }
    }
}
