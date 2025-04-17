using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WordBattleGame.Models
{
    public class Player
    {
        [Key]
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        [ForeignKey("PlayerStatsId")]
        public PlayerStats? Stats { get; set; }
    }
}