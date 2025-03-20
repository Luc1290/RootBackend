using System.Net.Http.Headers;
using System.Text.Json;

namespace RootBackend.Services;

public class ClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ClaudeService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        // Initialisation propre du HttpClient
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["Claude:ApiKey"]);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", _configuration["Claude:ApiVersion"]);
    }

    public async Task<string> GetCompletionAsync(string prompt)
    {
        var claudeRequest = new
        {
            model = _configuration["Claude:Model"],
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = 100
        };

        var response = await _httpClient.PostAsJsonAsync(_configuration["Claude:ApiUrl"], claudeRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            return $"Erreur Claude API : {response.StatusCode} - {errorDetails}";
        }

        var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        if (jsonDoc.RootElement.TryGetProperty("content", out var contentArray))
        {
            var firstContent = contentArray[0];
            if (firstContent.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString();
            }
        }

        return "Erreur : réponse invalide de Claude.";
    }


}
