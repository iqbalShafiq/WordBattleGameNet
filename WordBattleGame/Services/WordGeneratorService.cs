using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WordBattleGame.Repositories;

namespace WordBattleGame.Services
{
    public class WordGeneratorService(
        IWordHistoryRepository wordHistoryRepository,
        ILogger<WordGeneratorService> logger
    ) : IWordGeneratorService
    {
        private readonly HttpClient _httpClient = new();
        private readonly ILogger<WordGeneratorService> _logger = logger;
        private readonly string _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        private readonly IWordHistoryRepository _wordHistoryRepository = wordHistoryRepository;
        private readonly TimeSpan _historyPeriod = TimeSpan.FromDays(30);

        private static string BuildSystemPrompt(string difficulty, string language, IEnumerable<string> excludedWords)
        {
            return $"""
                You are a multilingual word generation assistant.
                Generate a single {difficulty} word in {language} with 5 to 10 letters (inclusive), suitable for a guessing game.
                The word must be a single, common word (not a phrase or compound word), and should not contain numbers, special characters, or punctuation.
                Exclude the following words: {string.Join(", ", excludedWords)}.
                Output only the word, and nothing else.
            """;
        }

        private static object BuildRequestBody(string systemPrompt, string prompt)
        {
            return new
            {
                model = "gpt-4.1-mini",
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };
        }

        public async Task<string> GenerateWordAsync(string language, string difficulty, IEnumerable<string> userIds)
        {
            string word = string.Empty;
            int maxAttempts = 5;
            int attempt = 0;
            
            var excludedWords = await _wordHistoryRepository.GetWordsByUserIdsAsync(userIds, _historyPeriod);
            _logger.LogInformation($"Excluded words for userIds [{string.Join(", ", userIds)}]: {string.Join(", ", excludedWords)}");

            do
            {
                var systemPrompt = BuildSystemPrompt(difficulty, language, excludedWords);
                var prompt = $"""
                    Generate a single {difficulty} word in {language} with 5 to 10 letters (inclusive), 
                    suitable for a guessing game. 
                    Only return the word, no explanation.
                """;
                var requestBody = BuildRequestBody(systemPrompt, prompt);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
                try
                {
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    word = doc.RootElement.GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? string.Empty;
                    word = word.Trim();
                    _logger.LogInformation($"Generated word: {word} (attempt {attempt + 1})");
                    excludedWords.Add(word);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating word from OpenAI API");
                    throw;
                }
                attempt++;
            }
            while (attempt < maxAttempts && await _wordHistoryRepository.ExistsAsync(word, userIds, _historyPeriod));

            await _wordHistoryRepository.InsertAsync(word, userIds, DateTime.UtcNow);
            return word;
        }
    }
}
