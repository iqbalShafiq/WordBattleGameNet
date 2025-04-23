using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public interface IPlayerRepository
    {
        Task<IEnumerable<Player>> GetAllAsync();
        Task<Player?> GetByIdAsync(string id);
        Task<List<Player>> GetByIdsAsync(IEnumerable<string> playerIds);
        Task UpdateStatsAsync(IEnumerable<string> playerIds);
        Task<PlayerStats> UpdateStatsAsync(string playerId, int score, bool? isWin);
    }
}
