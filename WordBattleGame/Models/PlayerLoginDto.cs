namespace WordBattleGame.Models
{
    public class PlayerLoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class PasswordResetRequestDto
    {
        public required string Email { get; set; }
    }

    public class PasswordResetDto
    {
        public required string Email { get; set; }
        public required string Token { get; set; }
        public required string NewPassword { get; set; }
    }
}