namespace WordBattleGame.Models
{
    public class RoundStartedDto
    {
        public string RoundId { get; set; } = string.Empty;
        public string GeneratedWord { get; set; } = string.Empty;
        public string TrueWord { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
    }
}