using Microsoft.EntityFrameworkCore;
using WordBattleGame.Data;
using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly AppDbContext _context;
        public GameRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Game?> GetByIdAsync(string id)
        {
            return await _context.Games.Include(g => g.Players).Include(g => g.Rounds).FirstOrDefaultAsync(g => g.Id == id);
        }
        public async Task<IEnumerable<Game>> GetAllAsync()
        {
            return await _context.Games.Include(g => g.Players).Include(g => g.Rounds).ToListAsync();
        }
        public async Task AddAsync(Game game)
        {
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Game game)
        {
            _context.Entry(game).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
