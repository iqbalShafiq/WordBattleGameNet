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
            if (await _context.Players.AnyAsync(p => p.Email == dto.Email))
                return null;
            var player = new Player
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Score = 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.Players.Add(player);
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
    }
}
