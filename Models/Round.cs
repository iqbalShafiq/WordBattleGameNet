using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WordBattleGame.Models
{
    public class Round
    {
        [Key]
        public required string Id { get; set; }
        public required string GameId { get; set; }
        [ForeignKey("GameId")]
        public Game Game { get; set; } = default!;
        public int RoundNumber { get; set; }
        public string GeneratedWord { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string TrueWord { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? WinnerId { get; set; }
        [ForeignKey("WinnerId")]
        public Player? Winner { get; set; }
    }
}
