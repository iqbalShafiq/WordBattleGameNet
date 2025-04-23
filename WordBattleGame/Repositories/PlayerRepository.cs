using Microsoft.EntityFrameworkCore;
using WordBattleGame.Data;
using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public class PlayerRepository(AppDbContext context) : IPlayerRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<IEnumerable<Player>> GetAllAsync()
        {
            return await _context.Players.ToListAsync();
        }
        public async Task<Player?> GetByIdAsync(string id)
        {
            return await _context.Players.FindAsync(id);
        }

        public async Task<List<Player>> GetByIdsAsync(IEnumerable<string> playerIds)
        {
            return await _context.Players.Where(p => playerIds.Contains(p.Id)).ToListAsync();
        }

        public async Task UpdateStatsAsync(IEnumerable<string> playerIds)
        {
            await _context.PlayerStats
                .Where(ps => playerIds.Contains(ps.PlayerId))
                .ForEachAsync(ps => ps.TotalGames++);
            await _context.SaveChangesAsync();
        }

        public async Task<PlayerStats> UpdateStatsAsync(string playerId, int score, bool? isWin)
        {
            var playerStats = await _context.PlayerStats.FindAsync(playerId) ?? throw new Exception("Player stats not found");
            playerStats.TotalScore += score;
            if (isWin == null) playerStats.Draw++;
            else
            {
                if ((bool)isWin) playerStats.Win++;
                else playerStats.Lose++;
            }

            await _context.SaveChangesAsync();
            return playerStats;
        }
    }
}
