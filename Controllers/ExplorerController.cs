using Microsoft.AspNetCore.Mvc;
using RootBackend.Explorer.Services; // Ensure this namespace is correct
using RootBackend.Explorer.Models;
using System.Collections.Generic;
using System.Linq;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/explorer")]
    public class ExplorerController : ControllerBase
    {
        private readonly WeatherExplorer _weatherExplorer;

        public ExplorerController(WeatherExplorer weatherExplorer)
        {
            _weatherExplorer = weatherExplorer;
        }

        /// <summary>
        /// Donne la météo actuelle pour une ville donnée (via OpenMeteo).
        /// Exemple : /api/explorer/weather?city=Marseille
        /// </summary>
        [HttpGet("weather")]
        public async Task<IActionResult> GetWeather([FromQuery] string city, [FromQuery] bool forecast = false)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("Paramètre 'city' manquant.");

            var result = await _weatherExplorer.ExploreWeatherAsync(city, forecast);

            if (result == null)
                return NotFound($"Météo non trouvée pour la ville : {city}");

            if (forecast && result.Forecasts != null && result.Forecasts.Any())
            {
                // Retourner la météo actuelle avec les prévisions
                return Ok(new
                {
                    city = result.City,
                    temperature = result.Temperature,
                    wind = result.WindSpeed,
                    message = result.Condition,
                    forecasts = result.Forecasts.Select(f => new
                    {
                        date = f.Date.ToString("yyyy-MM-dd"),
                        day = f.Date.ToString("dddd", new System.Globalization.CultureInfo("fr-FR")),
                        min_temp = f.MinTemperature,
                        max_temp = f.MaxTemperature,
                        wind = f.WindSpeed,
                        condition = f.Condition,
                        precipitation_probability = f.PrecipitationProbability
                    }).ToList()
                });
            }
            else
            {
                // Retourner uniquement la météo actuelle (format d'origine)
                return Ok(new
                {
                    city = result.City,
                    temperature = result.Temperature,
                    wind = result.WindSpeed,
                    message = result.Condition
                });
            }
        }
    }
}