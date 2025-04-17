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
    }
}
