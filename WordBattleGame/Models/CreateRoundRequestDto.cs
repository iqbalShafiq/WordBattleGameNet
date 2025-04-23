namespace WordBattleGame.Models
{
    public class CreateRoundRequestDto
    {
        public required string GameId { get; set; } 
        public string Language { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
    }
}
