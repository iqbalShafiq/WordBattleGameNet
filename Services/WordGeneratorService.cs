using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WordBattleGame.Services
{
    public class WordGeneratorService(IConfiguration config) : IWordGeneratorService
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _openAiApiKey = config["OpenAI:ApiKey"] ?? string.Empty;

        public async Task<string> GenerateWordAsync(string language, string difficulty)
        {
            var systemPrompt = $"You are a multilingual word generation assistant. Generate a single {difficulty} word in {language} with 5-10 letters, suitable for a guessing game. Only return the word like 'Headset' (without quotes), no explanation.";
            var prompt = $"Generate a single {difficulty} word in {language} with 5-10 letters, suitable for a guessing game. Only return the word, no explanation.";
            var requestBody = new
            {
                model = "gpt-4.1-mini",
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var word = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return word?.Trim() ?? string.Empty;
        }
    }
}
