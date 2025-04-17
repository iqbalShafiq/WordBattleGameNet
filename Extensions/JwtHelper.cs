using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WordBattleGame.Models;

namespace WordBattleGame.Extensions
{
    public static class JwtHelper
    {
        public static string GenerateToken(Player player, IConfiguration config)
        {
            // Ambil dari environment variable, bukan config
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, player.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, player.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, player.Name)
            };
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
            var expiresInMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_MINUTES") ?? "60");
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
