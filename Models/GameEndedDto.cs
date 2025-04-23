using System.Collections.Generic;

namespace WordBattleGame.Models
{
    public class GameEndedDto
    {
        public List<PlayerDetailDto> Players { get; set; } = new();
    }
}