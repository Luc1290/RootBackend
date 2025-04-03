using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RootBackend.Explorer.Services
{
    public class WebScraperService
    {
        private readonly HttpClient _http;

        public WebScraperService(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient();
        }

        public async Task<(string Url, string Content)> ScrapeAsync(string query)
        {
            var payload = new { query };
            var json = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync("https://root-web-scraper.fly.dev/scrape", json);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ScrapeResult>(body);

                return (data?.Url ?? "", data?.Content ?? "Aucun contenu.");
            }
            catch
            {
                return ("", "Erreur de scraping. Le service est peut-être indisponible.");
            }
        }

        private class ScrapeResult
        {
            public string Url { get; set; } = "";
            public string Content { get; set; } = "";
        }
    }
}
