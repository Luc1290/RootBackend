using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RootBackend.Models;

namespace RootBackend.Services
{
    public class NlpService
    {
        private readonly HttpClient _httpClient;

        public NlpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<NlpResponse?> AnalyzeAsync(string question)
        {
            var request = new { question };

            var response = await _httpClient.PostAsJsonAsync("https://root-nlp.fly.dev/analyze", request);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<NlpResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
