using Microsoft.AspNetCore.Mvc;
using WordBattleGame.Models;
using WordBattleGame.Extensions;
using WordBattleGame.Repositories;
using Microsoft.AspNetCore.Authorization;
using WordBattleGame.Services;

namespace WordBattleGame.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class AuthController(IAuthRepository authRepository, IConfiguration config, IEmailService emailService) : ControllerBase
    {
        private readonly IAuthRepository _authRepository = authRepository;
        private readonly IConfiguration _config = config;
        private readonly IEmailService _emailService = emailService;

        [HttpPost("register")]
        public async Task<ActionResult<PlayerDto>> Register(
            [FromBody] PlayerRegisterDto dto
        )
        {
            var player = await _authRepository.RegisterAsync(dto);
            if (player == null) return BadRequest(new ErrorResponseDto { Message = "Email already registered.", Code = 400 });
            var playerDto = new PlayerDto
            {
                Id = player.Id,
                Name = player.Name,
                Email = player.Email,
                CreatedAt = player.CreatedAt
            };

            // Generate confirmation link with token
            var token = player.EmailConfirmationToken ?? string.Empty;
            var confirmationLink = $"{Request.Scheme}://localhost:5173/login?email={player.Email}&confirmationToken={Uri.EscapeDataString(token)}";
            await _emailService.SendConfirmationEmailAsync(player.Email, confirmationLink);
            return CreatedAtAction("GetPlayer", "Players", new { id = player.Id }, new ApiResponse<PlayerDto>(playerDto, "Register success, please check your email to confirm.", 201));
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(
            [FromBody] PlayerLoginDto dto
        )
        {
            var (player, error) = await _authRepository.LoginAsync(dto);
            if (player == null)
                return Unauthorized(new ErrorResponseDto { Message = error ?? "Login failed.", Code = 401 });
            var token = JwtHelper.GenerateToken(player, _config);

            var refreshToken = await _authRepository.GenerateRefreshTokenAsync(player);
            if (refreshToken == null)
                return Unauthorized(new ErrorResponseDto { Message = "Failed to generate refresh token.", Code = 401 });

            // Set access token cookie
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15),
            });

            // Set refresh token cookie
            Response.Cookies.Append("refresh_token", refreshToken.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7),
            });

            var response = new LoginResponseDto
            {
                Token = token,
                Player = new PlayerDto
                {
                    Id = player.Id,
                    Name = player.Name,
                    Email = player.Email,
                    IsEmailConfirmed = player.IsEmailConfirmed,
                    CreatedAt = player.CreatedAt
                },
                RefreshToken = refreshToken?.Token ?? string.Empty
            };
            return Ok(new ApiResponse<LoginResponseDto>(response, "Login success", 200));
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<object>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new ErrorResponseDto { Message = "Refresh token not found.", Code = 401 });

            var (token, newRefreshToken, error) = await _authRepository.RefreshTokenAsync(refreshToken);
            if (token == null || newRefreshToken == null)
                return Unauthorized(new ErrorResponseDto { Message = error ?? "Invalid refresh token.", Code = 401 });

            // Set access token cookie
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15),
            });

            // Set refresh token cookie
            Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7),
            });

            return Ok(new ApiResponse<object>(new { token, refreshToken = newRefreshToken }, "Refresh token success", 200));
        }

        [HttpPut("update-profile/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(
            [FromRoute] string id,
            [FromBody] UpdateProfileDto dto
        )
        {
            var success = await _authRepository.UpdateProfileAsync(id, dto);
            if (!success) return NotFound(new ErrorResponseDto { Message = "Player not found.", Code = 404 });
            return Ok(new ApiResponse<object>(null, "Profile updated", 200));
        }

        [HttpPut("change-password/{id}")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            [FromRoute] string id,
            [FromBody] ChangePasswordDto dto
        )
        {
            var (success, error) = await _authRepository.ChangePasswordAsync(id, dto);
            if (!success) return BadRequest(new ErrorResponseDto { Message = error ?? "Change password failed.", Code = 400 });
            return Ok(new ApiResponse<object>(null, "Password changed", 200));
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ErrorResponseDto { Message = "User not authenticated.", Code = 401 });
            return Ok(new ApiResponse<object>(null, "Authenticated", 200));
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            Response.Cookies.Append("access_token", "", new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            Response.Cookies.Append("refresh_token", "", new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            return Ok(new ApiResponse<object>(null, "Logout success", 200));
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token)
        {
            var success = await _authRepository.ConfirmEmailAsync(email, token);
            if (!success)
                return BadRequest(new ErrorResponseDto { Message = "Invalid, expired, or already confirmed email.", Code = 400 });
            return Ok(new ApiResponse<object>(null, "Email confirmed successfully.", 200));
        }

        [HttpPost("request-password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto dto)
        {
            var found = await _authRepository.GeneratePasswordResetTokenAsync(dto.Email);
            if (!found)
                return NotFound(new ErrorResponseDto { Message = "Email not found.", Code = 404 });
            var player = await _authRepository.LoginAsync(new PlayerLoginDto { Email = dto.Email, Password = "" });
            // Ambil token terbaru dari database
            var dbPlayer = player.player;
            if (dbPlayer == null)
                return NotFound(new ErrorResponseDto { Message = "Email not found.", Code = 404 });
            var token = dbPlayer.EmailConfirmationToken ?? string.Empty;
            var confirmationLink = $"{Request.Scheme}://localhost:5173/reset-password?email={dbPlayer.Email}&token={Uri.EscapeDataString(token)}";
            await _emailService.SendConfirmationEmailAsync(dbPlayer.Email, confirmationLink);
            return Ok(new ApiResponse<object>(null, "Password reset link sent to your email.", 200));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto dto)
        {
            var success = await _authRepository.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
            if (!success)
                return BadRequest(new ErrorResponseDto { Message = "Invalid, expired, or used token.", Code = 400 });
            return Ok(new ApiResponse<object>(null, "Password has been reset successfully.", 200));
        }
    }
}
