using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RootBackend.Models;

namespace RootBackend.Services
{
    public class WebScraperService
    {
        private readonly HttpClient _httpClient;

        public WebScraperService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ScraperResponse?> GetScrapedAnswerAsync(string searchQuery)
        {
            var payload = new { query = searchQuery };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://root-web-scraper.fly.dev/scrape", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return JsonSerializer.Deserialize<ScraperResponse>(json, options);
        }
    }
}
