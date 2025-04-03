using RootBackend.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RootBackend.Explorer.Skills
{
    public class MeteoSkill : IRootSkill
    {
        private readonly GroqService _groq;
        private readonly HttpClient _httpClient;

        public MeteoSkill(GroqService groq, IHttpClientFactory httpClientFactory)
        {
            _groq = groq;
            _httpClient = httpClientFactory.CreateClient();
        }

        public bool CanHandle(string message)
        {
            var lower = message.ToLower();
            return lower.Contains("météo") || lower.Contains("temps à") || lower.Contains("quel temps");
        }

        public async Task<string?> HandleAsync(string message)
        {
            try
            {
                // Étape 1 : envoyer au scraper
                var payload = new { query = message };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://root-web-scraper.fly.dev/scrape", content);

                if (!response.IsSuccessStatusCode)
                    return "Impossible de récupérer la météo pour le moment. 🌧️";

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var pageText = doc.RootElement.GetProperty("text").GetString();

                if (string.IsNullOrEmpty(pageText))
                    return "Je n’ai pas pu analyser la météo 😓.";

                // Étape 2 : demander à Groq de résumer intelligemment
                var answer = await _groq.AnalyzeHtmlAsync(pageText, message);

                return answer ?? "Je n’ai pas pu analyser la météo 😓.";
            }
            catch
            {
                return "Erreur inattendue lors de l’analyse météo. Essaie encore dans un instant.";
            }
        }

    }
}
