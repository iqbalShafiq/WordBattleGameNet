namespace WordBattleGame.Models
{
    public class GenerateWordRequestDto
    {
        public string Language { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
    }
}
