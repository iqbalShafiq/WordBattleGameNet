namespace WordBattleGame.Models
{
    public class AnswerSubmittedDto
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}