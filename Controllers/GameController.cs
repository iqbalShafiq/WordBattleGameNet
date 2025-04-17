using Microsoft.AspNetCore.Mvc;
using WordBattleGame.Models;
using WordBattleGame.Services;
using WordBattleGame.Repositories;

namespace WordBattleGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController(IWordGeneratorService wordGeneratorService, IGameRepository gameRepository, IRoundRepository roundRepository) : ControllerBase
    {
        private readonly IWordGeneratorService _wordGeneratorService = wordGeneratorService;
        private readonly IGameRepository _gameRepository = gameRepository;
        private readonly IRoundRepository _roundRepository = roundRepository;

        [HttpGet("generate-word")]
        public async Task<ActionResult<GeneratedWordResponseDto>> GenerateWord([FromQuery] string language, [FromQuery] string difficulty)
        {
            var trueWord = await _wordGeneratorService.GenerateWordAsync(language, difficulty);
            var generatedWord = ShuffleWord(trueWord);
            var response = new GeneratedWordResponseDto
            {
                GeneratedWord = generatedWord,
                TrueWord = trueWord
            };
            return Ok(response);
        }

        [HttpPost("create-round")]
        public async Task<ActionResult<Round>> CreateRound([FromQuery] string gameId, [FromQuery] string language, [FromQuery] string difficulty)
        {
            var game = await _gameRepository.GetByIdAsync(gameId);
            if (game == null) return NotFound("Game not found");

            var trueWord = await _wordGeneratorService.GenerateWordAsync(language, difficulty);
            var generatedWord = ShuffleWord(trueWord);

            var roundNumber = (game.Rounds?.Count ?? 0) + 1;

            var round = new Round
            {
                Id = Guid.NewGuid().ToString(),
                GameId = gameId,
                RoundNumber = roundNumber,
                GeneratedWord = generatedWord,
                TrueWord = trueWord,
                Difficulty = difficulty,
                Language = language
            };
            await _roundRepository.AddAsync(round);
            return CreatedAtAction(nameof(CreateRound), new { id = round.Id }, round);
        }

        private static string ShuffleWord(string word)
        {
            var chars = word.ToCharArray();
            var rng = new Random();
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }
    }
}