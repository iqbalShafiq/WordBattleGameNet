using Microsoft.EntityFrameworkCore;
using WordBattleGame.Data;
using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public class WordHistoryRepository : IWordHistoryRepository
    {
        private readonly AppDbContext _context;

        public WordHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(string word, IEnumerable<string> userIds, TimeSpan period)
        {
            var since = DateTime.UtcNow - period;
            return await _context.WordHistories
                .AnyAsync(wh => wh.Word == word
                    && userIds.Contains(wh.UserId)
                    && wh.Timestamp >= since);
        }

        public async Task InsertAsync(string word, IEnumerable<string> userIds, DateTime timestamp)
        {
            var entries = userIds.Select(userId => new WordHistory
            {
                Word = word,
                UserId = userId,
                Timestamp = timestamp
            });
            await _context.WordHistories.AddRangeAsync(entries);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetWordsByUserIdsAsync(IEnumerable<string> userIds, TimeSpan period)
        {
            var since = DateTime.UtcNow - period;
            return await _context.WordHistories
                .Where(wh => userIds.Contains(wh.UserId) && wh.Timestamp >= since)
                .Select(wh => wh.Word)
                .Distinct()
                .ToListAsync();
        }
    }
}
