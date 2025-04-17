namespace WordBattleGame.Services
{
    public interface IWordGeneratorService
    {
        Task<string> GenerateWordAsync(string language, string difficulty);
    }
}
