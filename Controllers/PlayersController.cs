using Microsoft.AspNetCore.Mvc;
using WordBattleGame.Models;
using WordBattleGame.Repositories;

namespace WordBattleGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController(IPlayerRepository playerRepository) : ControllerBase
    {
        private readonly IPlayerRepository _playerRepository = playerRepository;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Player>>> GetPlayers()
        {
            var players = await _playerRepository.GetAllAsync();
            return Ok(players);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Player>> GetPlayer(string id)
        {
            var player = await _playerRepository.GetByIdAsync(id);
            if (player == null) return NotFound();
            return Ok(player);
        }
    }
}