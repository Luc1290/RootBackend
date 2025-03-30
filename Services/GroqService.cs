using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RootBackend.Core;

namespace RootBackend.Services;

public class GroqService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

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

    public async Task<string> GetCompletionAsync(string message)
    {
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

     

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return $"Erreur Groq API : {response.StatusCode} - {responseContent[..Math.Min(100, responseContent.Length)]}...";
        }

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

    // Ajouter dans GroqService.cs
    public async Task<string> ExtractEntityAsync(string message, string entityType)
    {
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

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Erreur Groq API : {response.StatusCode} - {responseContent[..Math.Min(100, responseContent.Length)]}...");
            return string.Empty;
        }

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

}
