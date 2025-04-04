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

        public async Task<(string Url, string Content)> ScrapeAsync(string query, string intention)
        {
            // Construire un prompt en fonction de l’intention détectée
            string analysisPrompt = intention switch
            {
                "weather" => $"Tu dois EXTRAIRE les informations météo à partir de cette page web. Ignore les éléments inutiles. Résume uniquement la température, le vent, les conditions générales et les prévisions s’il y en a.",
                "news" => $"Analyse cette page web et EXTRAIS les actualités récentes (titres, résumés). Ne donne pas ton avis, juste les infos factuelles.",
                "restaurants" => $"Repère les noms, adresses et notes de restaurants si disponibles sur cette page. Résume sous forme de liste.",
                _ => $"Lis cette page et donne un résumé utile à l’utilisateur, en fonction de la question : \"{query}\"."
            };

            var payload = new
            {
                query,
                context = analysisPrompt
            };

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
