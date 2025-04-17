using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public interface IPlayerRepository
    {
        Task<IEnumerable<Player>> GetAllAsync();
        Task<Player?> GetByIdAsync(string id);
        Task<List<Player>> GetByIdsAsync(IEnumerable<string> playerIds);
    }
}
