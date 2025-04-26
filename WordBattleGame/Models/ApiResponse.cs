namespace WordBattleGame.Models
{
    public class ApiResponse<T>(T? data, string? message = null, int code = 200)
    {
        public T? Data { get; set; } = data;
        public string? Message { get; set; } = message;
        public int Code { get; set; } = code;
    }
}