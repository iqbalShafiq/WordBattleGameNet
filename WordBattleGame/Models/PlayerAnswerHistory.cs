using System;
using System.ComponentModel.DataAnnotations;

namespace WordBattleGame.Models
{
    public class PlayerAnswerHistory
    {
        [Key]
        public int Id { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string RoundId { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime Timestamp { get; set; }
    }
}