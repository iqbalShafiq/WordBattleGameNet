namespace WordBattleGame.Repositories
{
    public interface IWordHistoryRepository
    {
        Task<bool> ExistsAsync(string word, IEnumerable<string> userIds, TimeSpan period);
        Task InsertAsync(string word, IEnumerable<string> userIds, DateTime timestamp);
    }
}
