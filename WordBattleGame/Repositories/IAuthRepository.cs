using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public interface IAuthRepository
    {
        Task<Player?> RegisterAsync(PlayerRegisterDto dto);
        Task<(Player? player, string? error)> LoginAsync(PlayerLoginDto dto);
        Task<RefreshToken?> GenerateRefreshTokenAsync(Player player);
        Task<(string? token, string? refreshToken, string? error)> RefreshTokenAsync(string refreshToken);
        Task<bool> UpdateProfileAsync(string id, UpdateProfileDto dto);
        Task<(bool success, string? error)> ChangePasswordAsync(string id, ChangePasswordDto dto);
        Task<bool> ConfirmEmailAsync(string email, string token);
        Task<bool> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
        Task<Player?> GetByEmailAsync(string email);
    }
}
