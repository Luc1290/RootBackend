using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RootBackend.Models;

namespace RootBackend.Services
{
    public class NlpService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NlpService> _logger;
        private static readonly Dictionary<string, string> _intentCache = new Dictionary<string, string>();
        private const int MAX_CACHE_SIZE = 1000;

        public NlpService(HttpClient httpClient, ILogger<NlpService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<NlpResponse?> AnalyzeAsync(string question)
        {
            // Mise en cache simple - évite des appels répétés pour les mêmes questions
            if (_intentCache.TryGetValue(question, out string? cachedIntent))
            {
                _logger.LogInformation("Intent trouvé en cache pour la question: {Question}", question);
                return new NlpResponse { Intent = cachedIntent };
            }

            try
            {
                var request = new { question };

                _logger.LogInformation("Envoi de la requête NLP pour: {Question}", question);

                // Timeout plus court pour éviter de bloquer l'application
                var response = await _httpClient.PostAsJsonAsync("https://root-nlp.fly.dev/analyze", request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Échec de la requête NLP: {StatusCode}", response.StatusCode);
                    // En cas d'échec, retourner une intention par défaut
                    return new NlpResponse { Intent = "discussion" };
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Réponse NLP reçue: {Content}", content);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var nlpResponse = JsonSerializer.Deserialize<NlpResponse>(content, options);

                if (nlpResponse == null)
                {
                    _logger.LogWarning("Impossible de désérialiser la réponse NLP");
                    return new NlpResponse { Intent = "discussion" };
                }

                // Mise en cache de l'intent
                if (_intentCache.Count >= MAX_CACHE_SIZE)
                {
                    // Supprimer un élément aléatoirement si le cache est plein
                    var keys = new List<string>(_intentCache.Keys);
                    _intentCache.Remove(keys[new Random().Next(keys.Count)]);
                }
                _intentCache[question] = nlpResponse.Intent;

                return nlpResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erreur HTTP lors de l'analyse NLP: {Message}", ex.Message);
                return new NlpResponse { Intent = "discussion" };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout lors de l'analyse NLP: {Message}", ex.Message);
                return new NlpResponse { Intent = "discussion" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'analyse NLP: {Message}", ex.Message);
                return new NlpResponse { Intent = "discussion" };
            }
        }
    }
}