using System.Net;
using System.Text.Json;
using RootBackend.Core;

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
        var detectedLang = DetectLanguageLocally(prompt);
        Console.WriteLine($"🌍 Langue détectée : {detectedLang}");

        var fullPrompt = RootIdentity.BuildPrompt(prompt, detectedLang);
        var systemPrompt = RootIdentity.GetSystemPrompt();

        var claudeRequest = new
        {
            model = "claude-3-haiku-20240307",
            system = systemPrompt,
            messages = new[] { new { role = "user", content = fullPrompt } },
            max_tokens = 4090
        };

        var response = await _httpClient.PostAsJsonAsync(_configuration["Claude:ApiUrl"], claudeRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            return $"Erreur Claude API : {response.StatusCode} - {errorDetails[..Math.Min(100, errorDetails.Length)]}...";
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

            return $"Couldn't parse Claude response: {responseContent[..Math.Min(100, responseContent.Length)]}...";
        }
        catch (Exception ex)
        {
            return $"Error parsing Claude response: {ex.Message} - {responseContent[..Math.Min(100, responseContent.Length)]}...";
        }
    }

    private static string DetectLanguageLocally(string text)
    {
        var lower = text.ToLowerInvariant();

        if (lower.Contains("qui") || lower.Contains("pourquoi") || lower.Contains("toi") || lower.Contains("es-tu"))
            return "français";
        if (lower.Contains("who") || lower.Contains("what") || lower.Contains("are you") || lower.Contains("why"))
            return "anglais";
        if (lower.Contains("quién") || lower.Contains("eres") || lower.Contains("por qué"))
            return "espagnol";
        if (lower.Contains("wer") || lower.Contains("bist") || lower.Contains("warum"))
            return "allemand";
        if (lower.Contains("chi") || lower.Contains("sei") || lower.Contains("perché"))
            return "italien";

        return "français"; // fallback
    }

    private static string SanitizeHtml(string html)
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
