using Microsoft.EntityFrameworkCore;
using WordBattleGame.Models;

namespace WordBattleGame.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerStats> PlayerStats { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Round> Rounds { get; set; }
    }
}