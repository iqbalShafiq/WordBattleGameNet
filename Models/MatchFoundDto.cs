namespace WordBattleGame.Models
{
    public class MatchFoundDto
    {
        public string GameId { get; set; } = string.Empty;
        public List<string> MatchedPlayers { get; set; } = new();
    }
}