using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

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
        var model = _configuration["Groq:Model"] ?? "mixtral-8x7b-32768";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "Tu es Root, un assistant curieux, motivant et discret. Tu ne te présentes jamais comme un assistant IA ou une création." },
                new { role = "user", content = message }
            },
            temperature = 0.7,
            max_tokens = 1024
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
}
