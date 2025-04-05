using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RootBackend.Models;

namespace RootBackend.Services
{
    public class WebScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebScraperService> _logger;

        public WebScraperService(HttpClient httpClient, ILogger<WebScraperService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ScraperResponse?> GetScrapedAnswerAsync(string searchQuery)
        {
            try
            {
                _logger.LogInformation("Demande de scraping pour: {Query}", searchQuery);

                var payload = new { query = searchQuery };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("https://root-web-scraper.fly.dev/scrape", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Échec de la requête scraper: {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Réponse du scraper reçue ({Length} caractères)", json.Length);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    // Permet d'ignorer certains champs manquants
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var result = JsonSerializer.Deserialize<ScraperResponse>(json, options);

                if (result != null)
                {
                    // Vérifier le contenu et tronquer si nécessaire
                    if (result.Content?.Length > 50000)
                    {
                        _logger.LogWarning("Contenu scraper très long ({Length} chars), troncature appliquée", result.Content.Length);
                        result.Content = result.Content.Substring(0, 50000) + "...";
                    }

                    _logger.LogInformation("Scraping réussi: {Title}", result.Title);
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erreur HTTP lors du scraping: {Message}", ex.Message);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout lors du scraping: {Message}", ex.Message);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erreur de désérialisation JSON: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors du scraping: {Message}", ex.Message);
                return null;
            }
        }

        // Ajouter une nouvelle méthode pour récupérer plusieurs résultats
        public async Task<List<ScraperResponse>?> GetMultipleScrapedAnswersAsync(string searchQuery, int numResults = 3)
        {
            try
            {
                _logger.LogInformation("Demande de scraping multiple pour: {Query} ({NumResults} résultats)", searchQuery, numResults);

                var payload = new { query = searchQuery, numResults };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("https://root-web-scraper.fly.dev/scrape-multiple", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Échec de la requête scraper multiple: {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // La réponse a un format différent: { results: [ ... ] }
                var wrapper = JsonSerializer.Deserialize<MultipleScraperResponse>(json, options);

                if (wrapper?.Results == null || wrapper.Results.Count == 0)
                {
                    _logger.LogWarning("Aucun résultat trouvé dans la réponse du scraper multiple");
                    return null;
                }

                _logger.LogInformation("Scraping multiple réussi: {Count} résultats trouvés", wrapper.Results.Count);
                return wrapper.Results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du scraping multiple: {Message}", ex.Message);
                return null;
            }
        }
    }

    // Classe d'aide pour désérialiser la réponse du scraper multiple
    public class MultipleScraperResponse
    {
        public List<ScraperResponse> Results { get; set; } = new List<ScraperResponse>();
    }
}