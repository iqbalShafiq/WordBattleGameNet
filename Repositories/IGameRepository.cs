using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public interface IGameRepository
    {
        Task<Game?> GetByIdAsync(string id);
        Task<IEnumerable<Game>> GetAllAsync();
        Task<IEnumerable<Player>> GetExpectedPlayersAsync(string gameId);
        Task AddAsync(Game game);
        Task UpdateAsync(Game game);
    }
}
