using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public interface IRoundRepository
    {
        Task<Round?> GetByIdAsync(string id);
        Task<IEnumerable<Round>> GetAllAsync();
        Task AddAsync(Round round);
        Task UpdateAsync(Round round);
    }
}
