namespace WordBattleGame.Models
{
    public class GameScoreDto
    {
        public List<PlayerScoreDto> PlayerScores { get; set; } = new();
    }

    public class PlayerScoreDto
    {
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
    }
}