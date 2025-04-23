using Microsoft.AspNetCore.Mvc;
using WordBattleGame.Models;
using WordBattleGame.Extensions;
using WordBattleGame.Repositories;

namespace WordBattleGame.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class AuthController(IAuthRepository authRepository, IConfiguration config) : ControllerBase
    {
        private readonly IAuthRepository _authRepository = authRepository;
        private readonly IConfiguration _config = config;

        [HttpPost("register")]
        public async Task<ActionResult<PlayerDto>> Register(
            [FromBody] PlayerRegisterDto dto
        )
        {
            var player = await _authRepository.RegisterAsync(dto);
            if (player == null) return BadRequest("Email already registered.");
            var playerDto = new PlayerDto
            {
                Id = player.Id,
                Name = player.Name,
                Email = player.Email,
                CreatedAt = player.CreatedAt
            };
            return CreatedAtAction("GetPlayer", "Players", new { id = player.Id }, playerDto);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(
            [FromBody] PlayerLoginDto dto
        )
        {
            var (player, error) = await _authRepository.LoginAsync(dto);
            if (player == null)
                return Unauthorized(error);
            var token = JwtHelper.GenerateToken(player, _config);
            var refreshToken = await _authRepository.GenerateRefreshTokenAsync(player);
            var response = new LoginResponseDto
            {
                Token = token,
                Player = new PlayerDto
                {
                    Id = player.Id,
                    Name = player.Name,
                    Email = player.Email,
                    CreatedAt = player.CreatedAt
                },
                RefreshToken = refreshToken?.Token ?? string.Empty
            };
            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<object>> RefreshToken(
            [FromBody] RefreshTokenRequestDto dto
        )
        {
            var (token, newRefreshToken, error) = await _authRepository.RefreshTokenAsync(dto.RefreshToken);
            if (token == null)
                return Unauthorized(error);
            return Ok(new { token, refreshToken = newRefreshToken });
        }

        [HttpPut("update-profile/{id}")]
        public async Task<IActionResult> UpdateProfile(
            [FromRoute] string id,
            [FromBody] UpdateProfileDto dto
        )
        {
            var success = await _authRepository.UpdateProfileAsync(id, dto);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPut("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(
            [FromRoute] string id,
            [FromBody] ChangePasswordDto dto
        )
        {
            var (success, error) = await _authRepository.ChangePasswordAsync(id, dto);
            if (!success) return BadRequest(error);
            return NoContent();
        }
    }
}
