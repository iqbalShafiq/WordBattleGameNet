namespace WordBattleGame.Models
{
    public class PlayerDetailDto
    {
        public required string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public PlayerStatsDto? Stats { get; set; }
    }

    public class PlayerStatsDto
    {
        public int TotalGames { get; set; }
        public int TotalScore { get; set; }
        public int Win { get; set; }
        public int Lose { get; set; }
        public int Draw { get; set; }
    }
}
