using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RootBackend.Core;

namespace RootBackend.Services
{
    public class GroqService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _throttleSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly TimeSpan _minRequestInterval = TimeSpan.FromMilliseconds(500); // Minimum 500ms entre les requêtes

        public GroqService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("GROQ_API_KEY is missing in environment variables.");

            _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> GetCompletionAsync(string message, int maxRetries = 3)
        {
            // Acquérir le sémaphore pour limiter les requêtes
            await _throttleSemaphore.WaitAsync();
            try
            {
                // Appliquer le throttling
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < _minRequestInterval)
                {
                    await Task.Delay(_minRequestInterval - timeSinceLastRequest);
                }

                // Préparation de la requête comme dans votre code original
                var model = _configuration["Groq:Model"] ?? "mistral-saba-24b";
                var systemPrompt = RootIdentity.GetSystemPrompt();
                var fullPrompt = RootIdentity.BuildPrompt(message);

                var requestBody = new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = fullPrompt }
                    },
                    temperature = 0.7,
                    max_tokens = 32000
                };

                // Logique de retry avec exponential backoff
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                        // Enregistrer l'heure de la requête
                        _lastRequestTime = DateTime.UtcNow;
                        var response = await _httpClient.PostAsync("chat/completions", content);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(responseContent);
                                var completion = doc.RootElement
                                    .GetProperty("choices")[0]
                                    .GetProperty("message")
                                    .GetProperty("content")
                                    .GetString();

                                return completion ?? "Pas de réponse générée.";
                            }
                            catch (Exception ex)
                            {
                                return $"Erreur parsing réponse Groq : {ex.Message} - {responseContent[..Math.Min(100, responseContent.Length)]}...";
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            // Erreur de rate limiting - on réessaie avec backoff exponentiel
                            retryCount++;
                            if (retryCount > maxRetries)
                            {
                                return $"Erreur Groq API : TooManyRequests - Limite d'appels atteinte après {maxRetries} tentatives";
                            }

                            // Attendre avec backoff exponentiel
                            int delayMs = (int)Math.Pow(2, retryCount) * 1000; // 2, 4, 8 secondes...
                            Console.WriteLine($"Rate limiting rencontré, attente de {delayMs}ms avant nouvel essai ({retryCount}/{maxRetries})");
                            await Task.Delay(delayMs);
                            continue; // Retenter
                        }
                        else
                        {
                            // Autres types d'erreurs - on retourne l'erreur comme dans votre code original
                            return $"Erreur Groq API : {response.StatusCode} - {responseContent[..Math.Min(100, responseContent.Length)]}...";
                        }
                    }
                    catch (Exception ex)
                    {
                        // En cas d'exception, on augmente aussi le compteur de retry
                        retryCount++;
                        if (retryCount > maxRetries)
                        {
                            return $"Erreur inattendue après {maxRetries} tentatives: {ex.Message}";
                        }

                        // Attendre avant de réessayer
                        int delayMs = (int)Math.Pow(2, retryCount) * 1000;
                        Console.WriteLine($"Exception rencontrée: {ex.Message}, nouvel essai dans {delayMs}ms ({retryCount}/{maxRetries})");
                        await Task.Delay(delayMs);
                    }
                }
            }
            finally
            {
                // Toujours libérer le sémaphore
                _throttleSemaphore.Release();
            }
        }

        // Garder la méthode ExtractEntityAsync avec les mêmes améliorations
        public async Task<string> ExtractEntityAsync(string message, string entityType, int maxRetries = 3)
        {
            await _throttleSemaphore.WaitAsync();
            try
            {
                // Appliquer le throttling
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < _minRequestInterval)
                {
                    await Task.Delay(_minRequestInterval - timeSinceLastRequest);
                }

                var model = _configuration["Groq:Model"] ?? "mistral-saba-24b";
                var systemPrompt = $"Tu es un assistant spécialisé en extraction d'entités. Tu dois extraire uniquement le {entityType} mentionné dans le message de l'utilisateur. Réponds juste avec le nom de l'entité, sans phrase, sans formatage.";

                var requestBody = new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = message }
                    },
                    temperature = 0.1,
                    max_tokens = 50
                };

                // Logique de retry
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                        _lastRequestTime = DateTime.UtcNow;
                        var response = await _httpClient.PostAsync("chat/completions", content);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(responseContent);
                                var extraction = doc.RootElement
                                    .GetProperty("choices")[0]
                                    .GetProperty("message")
                                    .GetProperty("content")
                                    .GetString();

                                return extraction?.Trim() ?? string.Empty;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erreur parsing réponse Groq : {ex.Message}");
                                return string.Empty;
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            retryCount++;
                            if (retryCount > maxRetries)
                            {
                                Console.WriteLine($"Erreur Groq API : TooManyRequests - Limite d'appels atteinte après {maxRetries} tentatives");
                                return string.Empty;
                            }

                            int delayMs = (int)Math.Pow(2, retryCount) * 1000;
                            Console.WriteLine($"Rate limiting rencontré, attente de {delayMs}ms avant nouvel essai ({retryCount}/{maxRetries})");
                            await Task.Delay(delayMs);
                            continue;
                        }
                        else
                        {
                            Console.WriteLine($"Erreur Groq API : {response.StatusCode} - {responseContent[..Math.Min(100, responseContent.Length)]}...");
                            return string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount > maxRetries)
                        {
                            Console.WriteLine($"Erreur inattendue après {maxRetries} tentatives: {ex.Message}");
                            return string.Empty;
                        }

                        int delayMs = (int)Math.Pow(2, retryCount) * 1000;
                        await Task.Delay(delayMs);
                    }
                }
            }
            finally
            {
                _throttleSemaphore.Release();
            }
        }

        public async Task<string> AnalyzeHtmlAsync(string htmlContent, string userQuery)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return "Le contenu de la page est vide.";

            var prompt = $"""
Tu es un assistant intelligent. Voici le contenu extrait d'une page web (limité à 10 000 caractères). Résume les informations utiles en lien avec la question suivante.

# QUESTION UTILISATEUR
{userQuery}

# CONTENU DE LA PAGE
{htmlContent}

# RÉPONSE ATTENDUE
""";

            return await GetCompletionAsync(prompt);
        }

    }
}