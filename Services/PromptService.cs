using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RootBackend.Services
{
    public class PromptService
    {
        private readonly HttpClient _http;

        public PromptService(HttpClient httpClient)
        {
            _http = httpClient;
        }

        public async Task<string?> GeneratePromptAsync(string question, string intention, List<string> entities, string url, string content)
        {
            var payload = new
            {
                question,
                intention,
                entities,
                url,
                content
            };

            var json = JsonSerializer.Serialize(payload);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("https://root-nlp.fly.dev/prepare-groq-prompt", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PROMPT SERVICE] ❌ Erreur HTTP : {response.StatusCode}");
                return null;
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PromptResponse>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Prompt;
        }

        private class PromptResponse
        {
            public string Prompt { get; set; } = string.Empty;
        }
    }
}
