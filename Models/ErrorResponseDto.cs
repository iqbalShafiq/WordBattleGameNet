namespace WordBattleGame.Models
{
    public class ErrorResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int Code { get; set; }
    }
}