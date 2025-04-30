using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public interface IPlayerAnswerHistoryRepository
    {
        Task InsertAsync(PlayerAnswerHistory answerHistory);
        Task<List<PlayerAnswerHistory>> GetByGameIdAsync(string gameId);
        Task<List<PlayerAnswerHistory>> GetByRoundIdAsync(string roundId);
    }
}