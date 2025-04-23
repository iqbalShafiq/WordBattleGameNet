namespace WordBattleGame.Models
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public PlayerDto Player { get; set; } = default!;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class PlayerDto
    {
        public required string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
