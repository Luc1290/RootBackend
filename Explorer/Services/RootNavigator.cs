using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RootBackend.Explorer.Services
{
    public class RootNavigator
    {
        public async Task<string> ExplorePageAsync(string url)
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox" } // Important pour Fly.io
                });

                var context = await browser.NewContextAsync();
                var page = await context.NewPageAsync();
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 10000
                });

                var title = await page.TitleAsync();
                var bodyText = await page.Locator("body").InnerTextAsync();

                // Optionnel : nettoyage rapide (on peut améliorer plus tard)
                var cleanText = Regex.Replace(bodyText, @"\s{2,}", " ").Trim();

                return $"[NAVIGATE]\nTitre de la page : {title}\n\nContenu :\n{cleanText.Substring(0, Math.Min(3000, cleanText.Length))}";
            }
            catch (Exception ex)
            {
                return $"[NAVIGATE-ERROR]\nImpossible d'explorer la page : {ex.Message}";
            }
        }
    }
}
