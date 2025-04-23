namespace WordBattleGame.Models
{
    public class RoundEndedDto
    {
        public string TrueWord { get; set; } = string.Empty;
        public string? WinnerPlayerId { get; set; }
    }
}