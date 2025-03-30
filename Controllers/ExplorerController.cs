using Microsoft.AspNetCore.Mvc;
using RootBackend.Explorer.Services; // Ensure this namespace is correct
using RootBackend.Explorer.Models;

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
        public async Task<IActionResult> GetWeather([FromQuery] string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest("Paramètre 'city' manquant.");

            var result = await _weatherExplorer.ExploreWeatherAsync(city);

            if (result == null)
                return NotFound($"Météo non trouvée pour la ville : {city}");

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
