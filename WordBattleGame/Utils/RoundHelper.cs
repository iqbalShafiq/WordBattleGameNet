using Microsoft.AspNetCore.SignalR;
using WordBattleGame.Models;
using WordBattleGame.Repositories;
using WordBattleGame.Services;

namespace WordBattleGame.Utils
{
    public static class RoundHelper
    {
        private static readonly Dictionary<string, CancellationTokenSource> RoundCountdownTokens = new();

        public static async Task StartRoundAsync(
            IHubContext<Hubs.GameHub> hubContext,
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            string gameId,
            int roundNumber,
            string language,
            string difficulty,
            int countdownSeconds = 5,
            CancellationToken? externalToken = null)
        {
            using var scope = scopeFactory.CreateScope();
            var roundRepository = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var wordGeneratorService = scope.ServiceProvider.GetRequiredService<IWordGeneratorService>();

            var game = await gameRepository.GetByIdAsync(gameId);
            if (game == null)
            {
                logger.LogWarning($"Game {gameId} not found.");
                await hubContext.Clients.Group(gameId).SendAsync("GameNotFound", gameId);
                return;
            }

            var targetWord = await wordGeneratorService.GenerateWordAsync(language, difficulty, game.Players.Select(p => p.Id));
            var newGeneratedWord = WordUtils.ShuffleWord(targetWord);

            logger.LogInformation($"Starting round {roundNumber} for game {gameId}. Generated word: {newGeneratedWord}, Target word: {targetWord}");

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
            await roundRepository.AddAsync(newRound);
            await hubContext.Clients.Group(gameId).SendAsync("RoundStarted", new RoundStartedDto
            {
                RoundId = newRound.Id,
                GeneratedWord = newGeneratedWord,
                TrueWord = targetWord,
                RoundNumber = roundNumber
            });

            // Buat CTS baru untuk round ini
            var cts = new CancellationTokenSource();
            lock (RoundCountdownTokens)
            {
                if (RoundCountdownTokens.TryGetValue(newRound.Id, out var oldCts))
                {
                    oldCts.Cancel();
                    oldCts.Dispose();
                }
                RoundCountdownTokens[newRound.Id] = cts;
            }
            var token = cts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    for (int i = countdownSeconds; i >= 0; i--)
                    {
                        await hubContext.Clients.Group(gameId).SendAsync("CountdownTick", i);
                        await Task.Delay(1000, token);
                    }
                    using var innerScope = scopeFactory.CreateScope();
                    var innerRoundRepo = innerScope.ServiceProvider.GetRequiredService<IRoundRepository>();
                    var innerWordGen = innerScope.ServiceProvider.GetRequiredService<IWordGeneratorService>();
                    await EndRoundAsync(hubContext, scopeFactory, newRound.Id, logger);
                }
                catch (TaskCanceledException)
                {
                    logger.LogWarning($"Countdown for round {roundNumber} in game {gameId} was canceled.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error during countdown for round {roundNumber} in game {gameId}.");
                }
                finally
                {
                    lock (RoundCountdownTokens)
                    {
                        if (RoundCountdownTokens.TryGetValue(newRound.Id, out var toDispose))
                        {
                            toDispose.Dispose();
                            RoundCountdownTokens.Remove(newRound.Id);
                        }
                    }
                }
            });
        }

        public static async Task EndRoundAsync(
            IHubContext<Hubs.GameHub> hubContext,
            IServiceScopeFactory scopeFactory,
            string roundId,
            ILogger logger)
        {
            using var scope = scopeFactory.CreateScope();
            var roundRepository = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
            var round = await roundRepository.GetByIdWithGameAndPlayersAsync(roundId);
            logger.LogInformation($"Round {roundId} ended.");

            if (round == null) return;
            await hubContext.Clients.Group(round.GameId).SendAsync("RoundEnded", new RoundEndedDto
            {
                TrueWord = round.TrueWord,
                WinnerPlayerId = null
            });

            logger.LogInformation($"Round {round.RoundNumber} ended for game {round.GameId}.");

            if (round.Game != null && round.RoundNumber < round.Game.MaxRound)
            {
                logger.LogInformation($"Starting next round {round.RoundNumber + 1} for game {round.GameId}.");
                await StartRoundAsync(hubContext, scopeFactory, logger, round.GameId, round.RoundNumber + 1, round.Language, round.Difficulty);
            }
            else if (round.Game != null && round.RoundNumber == round.Game.MaxRound)
            {
                logger.LogInformation($"Game {round.GameId} ended. Calculating scores.");

                // Score calculation and game end logic
                var playerAnswerHistoryRepo = scope.ServiceProvider.GetRequiredService<IPlayerAnswerHistoryRepository>();
                var allAnswers = await playerAnswerHistoryRepo.GetByGameIdAsync(round.GameId);
                var playerScores = allAnswers
                    .GroupBy(a => a.PlayerId)
                    .Select(g => new { PlayerId = g.Key, TotalScore = g.Sum(a => a.Score) })
                    .ToList();
                var maxScore = playerScores.Max(x => x.TotalScore);

                // Cek draw: jika semua pemain memiliki skor sama
                bool isDraw = playerScores.All(x => x.TotalScore == maxScore) && playerScores.Count > 1;
                var winners = isDraw
                    ? []
                    : playerScores.Where(x => x.TotalScore == maxScore).Select(x => x.PlayerId).ToList();

                logger.LogInformation($"Game {round.GameId} ended. Winners: {string.Join(", ", winners)}.");

                // Update player stats
                var playerRepo = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
                logger.LogInformation($"Updating player stats for game {round.GameId}.");
                logger.LogInformation($"Total players: {round.Game.Players.Count}.");

                foreach (var player in round.Game.Players)
                {
                    logger.LogInformation($"Updating stats for player {player.Name} ({player.Id}).");
                    bool? isWin = null;
                    if (winners.Count == 0) isWin = null;
                    else { isWin = winners.Contains(player.Id); }

                    logger.LogInformation($"Player {player.Name} ({player.Id}) score: {playerScores.FirstOrDefault(x => x.PlayerId == player.Id)?.TotalScore ?? 0}.");

                    var playerScore = playerScores.FirstOrDefault(x => x.PlayerId == player.Id)?.TotalScore ?? 0;
                    await playerRepo.UpdateStatsAsync(player.Id, playerScore, isWin);

                    logger.LogInformation($"Player {player.Name} ({player.Id}) stats updated.");
                }

                // Send game end notification to all players
                await hubContext.Clients.Group(round.GameId).SendAsync("GameEnded", new GameEndedDto
                {
                    Players = [.. round.Game.Players.Select(p => new PlayerDetailDto {
                        Id = p.Id,
                        Name = p.Name,
                        Email = p.Email
                    })],
                    WinnerPlayerIds = winners
                });

                // Remove players from the game group
                foreach (var player in round.Game.Players)
                {
                    if (Hubs.GameHub.PlayerConnections.TryGetValue(player.Id, out var connectionId))
                    {
                        await hubContext.Groups.RemoveFromGroupAsync(connectionId, round.GameId);
                    }
                }
            }
            else
            {
                logger.LogWarning($"Game {round.GameId} not found for round {round.RoundNumber}.");
            }
        }

        public static void CancelCountdown(string roundId)
        {
            lock (RoundCountdownTokens)
            {
                if (RoundCountdownTokens.TryGetValue(roundId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    RoundCountdownTokens.Remove(roundId);
                }
            }
        }
    }
}
