using Microsoft.AspNetCore.SignalR;
using WordBattleGame.Models;
using WordBattleGame.Repositories;
using WordBattleGame.Services;

namespace WordBattleGame.Utils
{
    public static class RoundHelper
    {
        // Simpan CTS per roundId
        private static readonly Dictionary<string, CancellationTokenSource> RoundCountdownTokens = new();

        public static async Task StartRoundAsync(
            IHubContext<Hubs.GameHub> hubContext,
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            string gameId,
            int roundNumber,
            string language,
            string difficulty,
            int countdownSeconds = 60,
            CancellationToken? externalToken = null)
        {
            using var scope = scopeFactory.CreateScope();
            var roundRepository = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
            var wordGeneratorService = scope.ServiceProvider.GetRequiredService<IWordGeneratorService>();

            var targetWord = await wordGeneratorService.GenerateWordAsync(language, difficulty);
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
            await hubContext.Clients.Group(gameId).SendAsync("RoundStarted", newRound.Id, newGeneratedWord, targetWord, roundNumber);

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
            var round = await roundRepository.GetByIdAsync(roundId);
            logger.LogInformation($"Round {roundId} ended.");

            if (round == null) return;
            await hubContext.Clients.Group(round.GameId).SendAsync("RoundEnded", round.TrueWord, null);

            logger.LogInformation($"Round {round.RoundNumber} ended for game {round.GameId}.");

            if (round.Game != null && round.RoundNumber < round.Game.MaxRound)
            {
                await StartRoundAsync(hubContext, scopeFactory, logger, round.GameId, round.RoundNumber + 1, round.Language, round.Difficulty);
            }
            else if (round.Game != null && round.RoundNumber == round.Game.MaxRound)
            {
                await hubContext.Clients.Group(round.GameId).SendAsync("GameEnded", round.Game.Players);
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
