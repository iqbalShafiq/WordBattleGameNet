using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using WordBattleGame.Data;
using WordBattleGame.Extensions;
using WordBattleGame.Models;

namespace WordBattleGame.Repositories
{
    public class AuthRepository(AppDbContext context, IConfiguration config) : IAuthRepository
    {
        private readonly AppDbContext _context = context;
        private readonly IConfiguration _config = config;

        public async Task<Player?> RegisterAsync(PlayerRegisterDto dto)
        {
            if (await _context.Players.AnyAsync(p => p.Email == dto.Email)) return null;
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var expiry = DateTime.UtcNow.AddHours(24);
            var player = new Player
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow,
                EmailConfirmationToken = token,
                EmailConfirmationTokenExpiry = expiry
            };
            _context.Players.Add(player);

            var playerStats = new PlayerStats
            {
                Id = Guid.NewGuid().ToString(),
                PlayerId = player.Id,
                TotalScore = 0,
                Win = 0,
                Lose = 0,
                Draw = 0
            };
            _context.PlayerStats.Add(playerStats);

            await _context.SaveChangesAsync();
            return player;
        }
        public async Task<(Player? player, string? error)> LoginAsync(PlayerLoginDto dto)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Email == dto.Email);
            if (player == null || !BCrypt.Net.BCrypt.Verify(dto.Password, player.PasswordHash))
                return (null, "Invalid email or password.");
            return (player, null);
        }
        public async Task<RefreshToken?> GenerateRefreshTokenAsync(Player player)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                PlayerId = player.Id
            };
            var oldTokens = _context.RefreshTokens.Where(r => r.PlayerId == player.Id);
            _context.RefreshTokens.RemoveRange(oldTokens);
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }
        public async Task<(string? token, string? refreshToken, string? error)> RefreshTokenAsync(string refreshToken)
        {
            var tokenEntity = await _context.RefreshTokens.Include(r => r.Player)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);
            if (tokenEntity == null || tokenEntity.ExpiryDate < DateTime.UtcNow)
                return (null, null, "Invalid or expired refresh token.");
            var newJwt = JwtHelper.GenerateToken(tokenEntity.Player, _config);
            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                PlayerId = tokenEntity.PlayerId
            };
            _context.RefreshTokens.Remove(tokenEntity);
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();
            return (newJwt, newRefreshToken.Token, null);
        }
        public async Task<bool> UpdateProfileAsync(string id, UpdateProfileDto dto)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return false;
            player.Name = dto.Name;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<(bool success, string? error)> ChangePasswordAsync(string id, ChangePasswordDto dto)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return (false, "Player not found.");
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, player.PasswordHash))
                return (false, "Current password is incorrect.");
            player.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return (true, null);
        }
        public async Task<bool> ConfirmEmailAsync(string email, string token)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Email == email);
            if (player == null)
            {
                Console.WriteLine($"[ConfirmEmail] Player not found: {email}");
                return false;
            }
            if (player.IsEmailConfirmed)
            {
                Console.WriteLine($"[ConfirmEmail] Email already confirmed: {email}");
                return false;
            }
            if (player.EmailConfirmationToken != token)
            {
                Console.WriteLine($"[ConfirmEmail] Token mismatch for {email}. Expected: {player.EmailConfirmationToken}, Got: {token}");
                return false;
            }
            if (player.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            {
                Console.WriteLine($"[ConfirmEmail] Token expired for {email}. Expiry: {player.EmailConfirmationTokenExpiry}, Now: {DateTime.UtcNow}");
                return false;
            }
            player.IsEmailConfirmed = true;
            player.EmailConfirmationToken = null;
            player.EmailConfirmationTokenExpiry = null;
            await _context.SaveChangesAsync();
            Console.WriteLine($"[ConfirmEmail] Email confirmed: {email}");
            return true;
        }

        public async Task<bool> GeneratePasswordResetTokenAsync(string email)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Email == email);
            if (player == null) return false;
            player.EmailConfirmationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            player.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Email == email);
            if (player == null) return false;
            if (player.EmailConfirmationToken != token || player.EmailConfirmationTokenExpiry < DateTime.UtcNow)
                return false;
            player.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            player.EmailConfirmationToken = null;
            player.EmailConfirmationTokenExpiry = null;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
