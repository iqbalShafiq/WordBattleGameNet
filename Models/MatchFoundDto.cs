namespace WordBattleGame.Models
{
    public class MatchFoundDto
    {
        public string GameId { get; set; } = string.Empty;
        public List<string> MatchedPlayerIds { get; set; } = new();
    }
}