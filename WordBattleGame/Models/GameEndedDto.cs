namespace WordBattleGame.Models
{
    public class GameEndedDto
    {
        public List<PlayerDetailDto> Players { get; set; } = [];
        public List<string> WinnerPlayerIds { get; set; } = [];
    }
}