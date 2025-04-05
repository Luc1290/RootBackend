using System;
using System.Threading.Tasks;
using RootBackend.Models;
using RootBackend.Services;

namespace RootBackend.Core
{
    public static class IntentRouter
    {
        public static async Task<string> HandleAsync(
            NlpResponse nlp,
            string userMessage,
            GroqService groq,
            WebScraperService scraper)
        {
            switch (nlp.Intent)
            {
                case "recherche_web":
                    var query = $"{nlp.Entities.GetValueOrDefault("type_info", "")} {nlp.Entities.GetValueOrDefault("lieu", "")} {nlp.Entities.GetValueOrDefault("date", "")}".Trim();
                    var scraped = await scraper.GetScrapedAnswerAsync(query);

                    if (string.IsNullOrWhiteSpace(scraped?.Content))
                        return "Désolé, je n’ai pas trouvé d’information fiable.";

                    var promptWeb = $"Voici une information trouvée en ligne. Résume-la pour un humain :\n\n{scraped.Content}";
                    return await groq.GetCompletionAsync(promptWeb);

                case "code":
                    var promptCode = $"Écris un code correspondant à cette demande : {nlp.Entities.GetValueOrDefault("description", userMessage)}";
                    return await groq.GetCompletionAsync(promptCode);

                case "dessin_image":
                    return $"Tu m’as demandé une image de : {nlp.Entities.GetValueOrDefault("description", "quelque chose")}. Génération d’image à venir.";

                default:
                    var promptDefault = $"L’utilisateur dit : \"{userMessage}\"\n\nRéponds naturellement, de façon utile.";
                    return await groq.GetCompletionAsync(promptDefault);
            }
        }
    }
}
