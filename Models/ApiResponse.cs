namespace WordBattleGame.Models
{
    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public string? Message { get; set; }
        public int Code { get; set; }
        public ApiResponse(T data, string? message = null, int code = 200)
        {
            Data = data;
            Message = message;
            Code = code;
        }
    }
}