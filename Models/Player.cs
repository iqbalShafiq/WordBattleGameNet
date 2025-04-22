using System.ComponentModel.DataAnnotations;

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
        public PlayerStats? Stats { get; set; }
        public ICollection<Game> Games { get; set; } = new List<Game>();
    }
}