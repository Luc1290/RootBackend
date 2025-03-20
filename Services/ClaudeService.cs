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
            prompt = prompt,
            max_tokens_to_sample = 100
        };

        var response = await _httpClient.PostAsJsonAsync(_configuration["Claude:ApiUrl"], claudeRequest);

        if (!response.IsSuccessStatusCode)
        {
            // Gestion explicite des erreurs API
            return $"Erreur Claude API : {response.StatusCode}";
        }

        var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        if (jsonDoc.RootElement.TryGetProperty("completion", out var completionElement))
        {
            return completionElement.GetString();
        }

        return "Erreur : réponse invalide de Claude.";
    }
}
