using Microsoft.EntityFrameworkCore;
using WordBattleGame.Data;
using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public class PlayerAnswerHistoryRepository(AppDbContext context) : IPlayerAnswerHistoryRepository
    {
        private readonly AppDbContext _context = context;

        public async Task InsertAsync(PlayerAnswerHistory answerHistory)
        {
            await _context.PlayerAnswerHistories.AddAsync(answerHistory);
            await _context.SaveChangesAsync();
        }
        public async Task<List<PlayerAnswerHistory>> GetByGameIdAsync(string gameId)
        {
            return await _context.PlayerAnswerHistories.Where(x => x.GameId == gameId).ToListAsync();
        }
        public async Task<List<PlayerAnswerHistory>> GetByRoundIdAsync(string roundId)
        {
            return await _context.PlayerAnswerHistories.Where(x => x.RoundId == roundId).ToListAsync();
        }
    }
}