using Microsoft.Playwright;

namespace RootBackend.Explorer.Services
{
    public class WebScraperService
    {
        public async Task<(string Url, string Content)> ScrapeFirstResultAsync(string query)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox" } // nécessaire sur Fly.io
            });

            var page = await browser.NewPageAsync();

            try
            {
                // 1. Recherche sur DuckDuckGo
                var searchUrl = $"https://duckduckgo.com/?q={Uri.EscapeDataString(query)}";
                await page.GotoAsync(searchUrl);
                await page.WaitForTimeoutAsync(1500);

                // 2. Clique sur le 1er lien
                var firstResultSelector = "a.result__a";
                await page.WaitForSelectorAsync(firstResultSelector, new PageWaitForSelectorOptions { Timeout = 5000 });
                var href = await page.GetAttributeAsync(firstResultSelector, "href");

                if (string.IsNullOrEmpty(href))
                    throw new Exception("Aucun lien trouvé dans les résultats.");

                // 3. Va sur le lien et extrait le contenu
                await page.GotoAsync(href, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await page.WaitForTimeoutAsync(2000); // Laisse le temps au contenu de charger

                var text = await page.EvaluateAsync<string>("() => document.body.innerText");
                return (href, text.Length > 10000 ? text.Substring(0, 10000) : text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur WebScraper : {ex.Message}");
                return ("", "Une erreur est survenue lors du scraping.");
            }
        }
    }
}
