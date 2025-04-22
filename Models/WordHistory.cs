using System.ComponentModel.DataAnnotations;

namespace WordBattleGame.Models
{
    public class WordHistory
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public DateTime UsedAt { get; set; }
    }
}
