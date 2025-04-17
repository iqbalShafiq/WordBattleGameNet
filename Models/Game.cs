using System.ComponentModel.DataAnnotations;

namespace WordBattleGame.Models
{
    public class Game
    {
        [Key]
        public required string Id { get; set; }
        public int MaxRound { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Player> Players { get; set; } = [];
        public ICollection<Round> Rounds { get; set; } = [];
    }
}
