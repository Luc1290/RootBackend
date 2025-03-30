using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RootBackend.Explorer.Models;

namespace RootBackend.Explorer.ApiClients
{
    public class OpenMeteoClient
    {
        private readonly HttpClient _httpClient;

        public OpenMeteoClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherResult?> GetCurrentWeatherAsync(double latitude, double longitude, string city)
        {
            // 🔒 Filtre de sécurité
            if (latitude == 0 || longitude == 0)
            {
                Console.WriteLine($"Coordonnées invalides pour {city} → latitude: {latitude}, longitude: {longitude}");
                return null;
            }

            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current_weather=true";
            Console.WriteLine("URL appel météo : " + url);

            var response = await _httpClient.GetAsync(url);

            // Sécurité : log si échec
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠️ L'API météo a renvoyé un code {response.StatusCode} pour {city}.");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<WeatherApiResponse>(json, options);


            var weather = result?.Current_weather;
            if (weather == null)
            {
                Console.WriteLine("❌ Aucune donnée météo dans la réponse JSON.");
                return null;
            }

            return new WeatherResult
            {
                City = city,
                Temperature = weather.Temperature,
                WindSpeed = weather.Windspeed,
                Condition = $"Il fait {weather.Temperature}°C avec un vent de {weather.Windspeed} km/h."
            };
        }


        private class WeatherApiResponse
        {
            public CurrentWeather? Current_weather { get; set; }
        }

        private class CurrentWeather
        {
            public double Temperature { get; set; }
            public double Windspeed { get; set; }
        }
    }
}
