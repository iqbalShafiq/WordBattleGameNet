using Microsoft.EntityFrameworkCore;
using WordBattleGame.Data;
using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public class RoundRepository : IRoundRepository
    {
        private readonly AppDbContext _context;
        public RoundRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Round?> GetByIdAsync(string id)
        {
            return await _context.Rounds.Include(r => r.Game).Include(r => r.Winner).FirstOrDefaultAsync(r => r.Id == id);
        }
        public async Task<IEnumerable<Round>> GetAllAsync()
        {
            return await _context.Rounds.Include(r => r.Game).Include(r => r.Winner).ToListAsync();
        }
        public async Task AddAsync(Round round)
        {
            _context.Rounds.Add(round);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Round round)
        {
            _context.Entry(round).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        public async Task<Round?> GetByIdWithGameAndPlayersAsync(string id)
        {
            return await _context.Rounds
                .Include(r => r.Game)
                    .ThenInclude(g => g.Players)
                .Include(r => r.Winner)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}
