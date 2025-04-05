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
                    var scraped = await scraper.GetScrapedAnswerAsync(userMessage);

                    if (string.IsNullOrWhiteSpace(scraped?.Content))
                        return "Désolé, je n’ai pas trouvé d’information fiable.";

                    var promptWeb = $"Voici une information trouvée en ligne. Résume-la pour un humain :\n\n{scraped.Content}";
                    return await groq.GetCompletionAsync(promptWeb);

                case "generation_code":
                    var promptCode = $"Écris un code correspondant à cette demande : {userMessage}";
                    return await groq.GetCompletionAsync(promptCode);

                case "generation_image":
                    return $"Tu m’as demandé une image de : {userMessage}. (Génération d’image à venir)";

                case "discussion":
                default:
                    var promptDefault = $"L’utilisateur dit : \"{userMessage}\"\n\nRéponds naturellement, de façon utile.";
                    return await groq.GetCompletionAsync(promptDefault);
            }
        }
    }
}
