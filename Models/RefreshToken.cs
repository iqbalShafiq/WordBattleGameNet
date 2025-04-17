using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WordBattleGame.Models
{
    public class RefreshToken
    {
        [Key]
        public required string Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public required string PlayerId { get; set; }
        [ForeignKey("PlayerId")]
        public Player Player { get; set; } = default!;
    }
}
