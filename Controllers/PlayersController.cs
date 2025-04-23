using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBattleGame.Models;
using WordBattleGame.Repositories;

namespace WordBattleGame.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PlayersController(IPlayerRepository playerRepository) : ControllerBase
    {
        private readonly IPlayerRepository _playerRepository = playerRepository;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlayerDetailDto>>> GetPlayers()
        {
            var players = await _playerRepository.GetAllAsync();
            var result = players.Select(p => new PlayerDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                Email = p.Email,
                CreatedAt = p.CreatedAt,
                Stats = p.Stats == null ? null : new PlayerStatsDto
                {
                    TotalGames = p.Stats.TotalGames,
                    TotalScore = p.Stats.TotalScore,
                    Win = p.Stats.Win,
                    Lose = p.Stats.Lose,
                    Draw = p.Stats.Draw
                }
            });
            return Ok(new ApiResponse<IEnumerable<PlayerDetailDto>>(result, "Get players success", 200));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PlayerDetailDto>> GetPlayer(string id)
        {
            var player = await _playerRepository.GetByIdAsync(id);
            if (player == null) return NotFound(new ErrorResponseDto { Message = "Player not found.", Code = 404 });
            var result = new PlayerDetailDto
            {
                Id = player.Id,
                Name = player.Name,
                Email = player.Email,
                CreatedAt = player.CreatedAt,
                Stats = player.Stats == null ? null : new PlayerStatsDto
                {
                    TotalGames = player.Stats.TotalGames,
                    TotalScore = player.Stats.TotalScore,
                    Win = player.Stats.Win,
                    Lose = player.Stats.Lose,
                    Draw = player.Stats.Draw
                }
            };
            return Ok(new ApiResponse<PlayerDetailDto>(result, "Get player success", 200));
        }
    }
}