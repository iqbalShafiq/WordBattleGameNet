using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WordBattleGame.Data;
using WordBattleGame.Models;
using WordBattleGame.Repositories;
using Xunit;

namespace WordBattleGame.Tests
{
    public class GameRepositoryTests
    {
        private static AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task AddAsync_ShouldAddGame()
        {
            using var context = GetInMemoryDbContext();
            var repo = new GameRepository(context);
            var game = new Game
            {
                Id = "game1",
                MaxRound = 5,
                CreatedAt = DateTime.UtcNow
            };
            await repo.AddAsync(game);
            var result = await context.Games.FindAsync("game1");
            Assert.NotNull(result);
            Assert.Equal(5, result.MaxRound);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnGameWithPlayersAndRounds()
        {
            using var context = GetInMemoryDbContext();
            var player = new Player
            {
                Id = "p1",
                Name = "Player1",
                Email = "p1@email.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };
            var round = new Round { Id = "r1", GameId = "g1" };
            var game = new Game
            {
                Id = "g1",
                MaxRound = 3,
                CreatedAt = DateTime.UtcNow,
                Players = new List<Player> { player },
                Rounds = new List<Round> { round }
            };
            context.Games.Add(game);
            await context.SaveChangesAsync();
            var repo = new GameRepository(context);
            var result = await repo.GetByIdAsync("g1");
            Assert.NotNull(result);
            Assert.Single(result.Players);
            Assert.Single(result.Rounds);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllGames()
        {
            using var context = GetInMemoryDbContext();
            context.Games.Add(new Game { Id = "g1", MaxRound = 2, CreatedAt = DateTime.UtcNow });
            context.Games.Add(new Game { Id = "g2", MaxRound = 4, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
            var repo = new GameRepository(context);
            var result = (await repo.GetAllAsync()).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateGame()
        {
            using var context = GetInMemoryDbContext();
            var game = new Game { Id = "g1", MaxRound = 2, CreatedAt = DateTime.UtcNow };
            context.Games.Add(game);
            await context.SaveChangesAsync();
            var repo = new GameRepository(context);
            game.MaxRound = 10;
            await repo.UpdateAsync(game);
            var updated = await context.Games.FindAsync("g1");
            Assert.Equal(10, updated!.MaxRound);
        }

        [Fact]
        public async Task GetExpectedPlayersAsync_ShouldReturnPlayers()
        {
            using var context = GetInMemoryDbContext();
            var player1 = new Player { Id = "p1", Name = "Player1", Email = "p1@email.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
            var player2 = new Player { Id = "p2", Name = "Player2", Email = "p2@email.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
            var game = new Game { Id = "g1", MaxRound = 3, CreatedAt = DateTime.UtcNow, Players = new List<Player> { player1, player2 } };
            context.Games.Add(game);
            await context.SaveChangesAsync();
            var repo = new GameRepository(context);
            var players = (await repo.GetExpectedPlayersAsync("g1")).ToList();
            Assert.Equal(2, players.Count);
            Assert.Contains(players, p => p.Id == "p1");
            Assert.Contains(players, p => p.Id == "p2");
        }
    }
}
