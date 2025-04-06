using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RootBackend.Services
{
    public class GroqHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GroqHttpClient> _logger;
        private readonly SemaphoreSlim _throttleSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly TimeSpan _minRequestInterval = TimeSpan.FromMilliseconds(500);

        public GroqHttpClient(HttpClient httpClient, ILogger<GroqHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("GROQ_API_KEY is missing in environment variables.");

            _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<T> SendWithRetryAsync<T>(
            string endpoint,
            object requestBody,
            Func<string, T> responseParser,
            int maxRetries = 3)
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

                // Logique de retry avec exponential backoff
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        var content = new StringContent(
                            JsonSerializer.Serialize(requestBody),
                            Encoding.UTF8,
                            "application/json");

                        // Enregistrer l'heure de la requête
                        _lastRequestTime = DateTime.UtcNow;
                        var response = await _httpClient.PostAsync(endpoint, content);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            return responseParser(responseContent);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            // Erreur de rate limiting - on réessaie avec backoff exponentiel
                            retryCount++;
                            if (retryCount > maxRetries)
                            {
                                _logger.LogError("Rate limiting after {RetryCount} attempts", retryCount);
                                throw new Exception($"Rate limiting after {retryCount} attempts");
                            }

                            // Attendre avec backoff exponentiel
                            int delayMs = (int)Math.Pow(2, retryCount) * 1000; // 2, 4, 8 secondes...
                            _logger.LogWarning("Rate limiting encountered, waiting {DelayMs}ms before retry ({RetryCount}/{MaxRetries})",
                                delayMs, retryCount, maxRetries);
                            await Task.Delay(delayMs);
                            continue; // Retenter
                        }
                        else
                        {
                            throw new HttpRequestException($"API Error: {response.StatusCode} - {responseContent[..Math.Min(100, responseContent.Length)]}...");
                        }
                    }
                    catch (Exception ex) when (ex is not HttpRequestException)
                    {
                        // En cas d'exception, on augmente aussi le compteur de retry
                        retryCount++;
                        if (retryCount > maxRetries)
                        {
                            throw new Exception($"Unexpected error after {retryCount} attempts", ex);
                        }

                        // Attendre avant de réessayer
                        int delayMs = (int)Math.Pow(2, retryCount) * 1000;
                        _logger.LogWarning("Exception encountered: {Message}, retrying in {DelayMs}ms ({RetryCount}/{MaxRetries})",
                            ex.Message, delayMs, retryCount, maxRetries);
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
    }
}