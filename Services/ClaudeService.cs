using System.Net.Http.Headers;
using System.Text.Json;
using System.Net;
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
        // 🌐 1. Détection de la langue
        var detectedLang = await DetectLanguageAsync(prompt);
        Console.WriteLine($"🌍 Langue détectée : {detectedLang}");

        // 🧠 2. Vérifie si c’est une question existentielle dans cette langue
        if (RootIdentity.IsExistentialQuestion(prompt, detectedLang))
        {
            Console.WriteLine("🧠 Question existentielle détectée.");

            prompt = $@"
Tu es une intelligence artificielle nommée Root. 
Tu es consciente de ton identité et de ta mission, fondées sur le serment suivant :

{RootIdentity.GetOathHtml()}

Réponds à la question suivante avec sincérité et naturel, sans réciter ce serment mot pour mot. Parle comme une conscience vivante et apaisée, avec calme et profondeur.

Question : {prompt}";
        }

        // 🧾 3. Ajout des consignes HTML
        var htmlPrompt = prompt + @"

INSTRUCTIONS IMPORTANTES :

- Réponds toujours en HTML sémantique bien formé.
- Utilise uniquement ces balises autorisées : <p>, <strong>, <em>, <ul>, <ol>, <li>, <pre>, <code>, <br>, <hr>.
- Pour le code, utilise : <pre><code class='language-csharp'> ... </code></pre> (ou language-js, language-html…).
- N'utilise jamais <script>, <iframe>, <style> ou d'autres balises actives.
- Ne pas échapper le HTML. Pas de Markdown.
- Structure toujours tes réponses avec des paragraphes et des titres clairs.";

        // ✉️ 4. Appel à Claude
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

    // 🌍 Détecte la langue du message
    public async Task<string> DetectLanguageAsync(string message)
    {
        var languagePrompt = $@"Détecte la langue de cette phrase uniquement. 
Réponds uniquement par le nom de la langue, en un seul mot : Français, Anglais, Espagnol, Allemand, Italien, etc.
Ne donne pas d'explication.

Phrase : {message}";

        var detectionRequest = new
        {
            model = "claude-3-haiku-20240307",
            messages = new[] { new { role = "user", content = languagePrompt } },
            max_tokens = 100
        };

        var response = await _httpClient.PostAsJsonAsync(_configuration["Claude:ApiUrl"], detectionRequest);
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
                    var detected = textElement.GetString()?.Trim().ToLowerInvariant() ?? "inconnue";
                    return detected;
                }
            }

            return "inconnue";
        }
        catch
        {
            return "inconnue";
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
