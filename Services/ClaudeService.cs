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

        // Proper HttpClient initialization
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["Claude:ApiKey"]);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> GetCompletionAsync(string prompt)

    {
        // Ajout d'une consigne HTML-friendly
        var htmlPrompt = prompt + "\n\nRéponds uniquement au format HTML clair et structuré. Utilise les balises <p>, <ul>, <ol>, <li>, <strong>, <em>, <pre>, <code> si besoin. Ne pas échapper le HTML. Ne réponds pas avec des ``` ou des balises Markdown.";

        var claudeRequest = new
        {
            model = "claude-3-haiku-20240307", // Updated to a current model name
            messages = new[] { new { role = "user", content = htmlPrompt } },
            max_tokens = 10000 // Increased token limit for more complete responses
        };

        var response = await _httpClient.PostAsJsonAsync(_configuration["Claude:ApiUrl"], claudeRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            return $"Erreur Claude API : {response.StatusCode} - {errorDetails}";
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        try
        {
            if (jsonDoc.RootElement.TryGetProperty("content", out var contentArray) &&
                contentArray.GetArrayLength() > 0)
            {
                var firstContent = contentArray[0];
                if (firstContent.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString() ?? "No text returned";
                }
            }

            // Fallback if the expected structure isn't found
            return $"Couldn't parse Claude response: {responseContent.Substring(0, Math.Min(100, responseContent.Length))}...";
        }
        catch (Exception ex)
        {
            return $"Error parsing Claude response: {ex.Message} - {responseContent.Substring(0, Math.Min(100, responseContent.Length))}...";
        }
    }
}