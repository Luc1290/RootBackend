using System.Threading.Tasks;
using RootBackend.Explorer.ApiClients;
using RootBackend.Explorer.Models;

namespace RootBackend.Explorer.Services
{
    public class WeatherExplorer
    {
        private readonly GeocodingClient _geoClient;
        private readonly OpenMeteoClient _meteoClient;

        public WeatherExplorer(GeocodingClient geoClient, OpenMeteoClient meteoClient)
        {
            _geoClient = geoClient;
            _meteoClient = meteoClient;
        }

        public async Task<WeatherResult?> ExploreWeatherAsync(string city, bool includeForecast = false)
        {
            var location = await _geoClient.GetCoordinatesAsync(city);
            if (location == null) return null;

            Console.WriteLine($"Demande de météo pour {location.Name} ({location.Latitude}, {location.Longitude})");

            var weather = await _meteoClient.GetCurrentWeatherAsync(
                location.Latitude,
                location.Longitude,
                location.Name,
                includeForecast);

            return weather;
        }
    }
}