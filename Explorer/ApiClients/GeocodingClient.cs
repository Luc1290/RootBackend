using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RootBackend.Explorer.Models;

namespace RootBackend.Explorer.ApiClients
{
    public class GeocodingClient
    {
        private readonly HttpClient _httpClient;

        public GeocodingClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CityLocation?> GetCoordinatesAsync(string city)
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={city}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Résultat géocodage brut : " + json); // Debug temporaire

            var result = JsonSerializer.Deserialize<GeoApiResponse>(json);

            if (result?.results == null || result.results.Count == 0)
            {
                Console.WriteLine("Aucune correspondance trouvée dans l'API de géocodage.");
                return null;
            }

            // 🔍 Priorité : une ville avec coordonnées valides, nom non vide, et pays reconnu
            var location = result.results
                .FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.name) &&
                    r.latitude != 0 &&
                    r.longitude != 0 &&
                    !string.IsNullOrWhiteSpace(r.country));

            if (location == null)
            {
                Console.WriteLine("Aucune ville exploitable trouvée dans la liste.");
                return null;
            }

            return new CityLocation
            {
                Name = location.name,
                Latitude = location.latitude,
                Longitude = location.longitude,
                Country = location.country
            };
        }

        private class GeoApiResponse
        {
            public List<GeoResult>? results { get; set; }
        }

        private class GeoResult
        {
            public required string name { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
            public required string country { get; set; }
            public required string country_code { get; set; }
        }
    }
}
