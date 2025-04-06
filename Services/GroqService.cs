using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RootBackend.Core;
using RootBackend.Models;
using System.Text.Json.Serialization;

namespace RootBackend.Services
{
    public class GroqService
    {
        private readonly GroqHttpClient _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GroqService> _logger;
        private readonly PromptService _promptService;

        public GroqService(
            GroqHttpClient client,
            IConfiguration configuration,
            ILogger<GroqService> logger,
            PromptService promptService)
        {
            _client = client;
            _configuration = configuration;
            _logger = logger;
            _promptService = promptService;
        }

        public async Task<string> GetCompletionAsync(string message, int maxRetries = 3)
        {
            var model = _configuration["Groq:Model"] ?? "mistral-saba-24b";
            var systemPrompt = RootIdentity.GetSystemPrompt();
            var fullPrompt = RootIdentity.BuildPrompt(message);

            var requestBody = new GroqCompletionRequest
            {
                Model = model,
                Messages = new[]
                {
                    new GroqMessage { Role = "system", Content = systemPrompt },
                    new GroqMessage { Role = "user", Content = fullPrompt }
                },
                Temperature = 0.7,
                MaxTokens = 32000
            };

            try
            {
                return await _client.SendWithRetryAsync(
                    "chat/completions",
                    requestBody,
                    responseContent =>
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        return doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString() ?? "Pas de réponse générée.";
                    },
                    maxRetries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion");
                return $"Désolé, j'ai rencontré un problème technique: {ex.Message}";
            }
        }

        public async Task<string> ExtractEntityAsync(string message, string entityType, int maxRetries = 3)
        {
            var model = _configuration["Groq:Model"] ?? "mistral-saba-24b";
            var systemPrompt = _promptService.GetEntityExtractionPrompt(entityType);

            var requestBody = new GroqCompletionRequest
            {
                Model = model,
                Messages = new[]
                {
                    new GroqMessage { Role = "system", Content = systemPrompt },
                    new GroqMessage { Role = "user", Content = message }
                },
                Temperature = 0.1,
                MaxTokens = 50
            };

            try
            {
                return await _client.SendWithRetryAsync(
                    "chat/completions",
                    requestBody,
                    responseContent =>
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        return doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString()?.Trim() ?? string.Empty;
                    },
                    maxRetries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting entity");
                return string.Empty;
            }
        }

        public async Task<string> AnalyzeHtmlAsync(string htmlContent, string userQuery)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return "Le contenu de la page est vide.";

            var prompt = _promptService.GetHtmlAnalysisPrompt(htmlContent, userQuery);
            return await GetCompletionAsync(prompt);
        }
    }

    // Classes de modèle pour les requêtes Groq
    public class GroqCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public GroqMessage[] Messages { get; set; } = Array.Empty<GroqMessage>();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    public class GroqMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}