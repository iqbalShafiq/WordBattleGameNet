using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WordBattleGame.Models
{
    public class PlayerStats
    {
        [Key]
        public required string Id { get; set; }
        public required string PlayerId { get; set; }
        [ForeignKey("PlayerId")]
        public Player Player { get; set; } = default!;
        public int TotalGames { get; set; }
        public int HighestScore { get; set; }
        public int Win { get; set; }
        public int Lose { get; set; }
        public int Draw { get; set; }
    }
}
