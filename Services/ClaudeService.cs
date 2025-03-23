using System.Net.Http.Headers;
using System.Text.Json;
using System.Net;

namespace RootBackend.Services;

public class ClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ClaudeService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["Claude:ApiKey"]);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> GetCompletionAsync(string prompt)
    {
        var htmlPrompt = prompt + @"

INSTRUCTIONS IMPORTANTES :

- Réponds toujours en HTML sémantique bien formé.
- Utilise uniquement ces balises autorisées : <p>, <strong>, <em>, <ul>, <ol>, <li>, <pre>, <code>, <br>, <hr>.
- Pour le code, utilise : <pre><code class='language-csharp'> ... </code></pre> (ou language-js, language-html…).
- N'utilise jamais <script>, <iframe>, <style> ou d'autres balises actives.
- Ne pas échapper le HTML. Pas de Markdown.
- Structure toujours tes réponses avec des paragraphes et des titres clairs.";

        var claudeRequest = new
        {
            model = "claude-3-haiku-20240307",
            messages = new[] { new { role = "user", content = htmlPrompt } },
            max_tokens = 4090
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
                    var decoded = WebUtility.HtmlDecode(textElement.GetString() ?? "No text returned");
                    return SanitizeHtml(decoded);
                }
            }

            return $"Couldn't parse Claude response: {responseContent.Substring(0, Math.Min(100, responseContent.Length))}...";
        }
        catch (Exception ex)
        {
            return $"Error parsing Claude response: {ex.Message} - {responseContent.Substring(0, Math.Min(100, responseContent.Length))}...";
        }
    }

    private string SanitizeHtml(string html)
    {
        return html
            .Replace("<script", "&lt;script")
            .Replace("</script", "&lt;/script")
            .Replace("<iframe", "&lt;iframe")
            .Replace("</iframe", "&lt;/iframe")
            .Replace("<style", "&lt;style")
            .Replace("</style", "&lt;/style")
            .Replace("onerror=", "")
            .Replace("onload=", "")
            .Replace("javascript:", "")
            .Replace("document.", "")
            .Replace("window.", "");
    }
}
