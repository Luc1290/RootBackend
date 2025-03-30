using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RootBackend.Explorer.Models;
using RootBackend.Explorer.Helpers;
using System;
using System.Collections.Generic;

namespace RootBackend.Explorer.ApiClients
{
    public class OpenMeteoClient
    {
        private readonly HttpClient _httpClient;

        public OpenMeteoClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherResult?> GetCurrentWeatherAsync(double latitude, double longitude, string city, bool includeForecast = false)
        {
            // 🔒 Filtre de sécurité
            if (latitude == 0 || longitude == 0)
            {
                Console.WriteLine($"Coordonnées invalides pour {city} → latitude: {latitude}, longitude: {longitude}");
                return null;
            }

            // Modification de l'URL pour inclure les prévisions quotidiennes si demandé
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current_weather=true";

            if (includeForecast)
            {
                url += "&daily=weathercode,temperature_2m_max,temperature_2m_min,precipitation_probability_max,windspeed_10m_max&timezone=auto";
            }

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

            // ✨ Convertit le weathercode en condition lisible
            var conditionText = WeatherConditionHelper.GetConditionDescription(weather.Weathercode);

            var weatherResult = new WeatherResult
            {
                City = city,
                Temperature = weather.Temperature,
                WindSpeed = weather.Windspeed,
                Condition = conditionText
            };

            // Ajouter les prévisions si disponibles
            if (includeForecast && result?.Daily != null)
            {
                weatherResult.Forecasts = ParseDailyForecasts(result.Daily);
            }


            return weatherResult;
        }

        private List<DailyForecast> ParseDailyForecasts(DailyData daily)
        {
            var forecasts = new List<DailyForecast>();

            // Vérifier que toutes les données nécessaires sont présentes
            if (daily.Time == null || daily.Weathercode == null ||
                daily.Temperature_2m_max == null || daily.Temperature_2m_min == null ||
                daily.Precipitation_probability_max == null || daily.Windspeed_10m_max == null)
            {
                Console.WriteLine("⚠️ Données quotidiennes incomplètes dans la réponse de l'API");
                return forecasts;
            }

            // S'assurer que tous les tableaux ont la même longueur
            int days = Math.Min(
                Math.Min(daily.Time.Length, daily.Weathercode.Length),
                Math.Min(
                    Math.Min(daily.Temperature_2m_max.Length, daily.Temperature_2m_min.Length),
                    Math.Min(daily.Precipitation_probability_max.Length, daily.Windspeed_10m_max.Length)
                )
            );

            for (int i = 0; i < days; i++)
            {
                DateTime.TryParse(daily.Time[i], out DateTime date);

                forecasts.Add(new DailyForecast
                {
                    Date = date,
                    Condition = WeatherConditionHelper.GetConditionDescription(daily.Weathercode[i]),
                    MaxTemperature = daily.Temperature_2m_max[i],
                    MinTemperature = daily.Temperature_2m_min[i],
                    PrecipitationProbability = daily.Precipitation_probability_max[i],
                    WindSpeed = daily.Windspeed_10m_max[i]
                });
            }

            return forecasts;
        }

        private class WeatherApiResponse
        {
            public CurrentWeather? Current_weather { get; set; }
            public DailyData? Daily { get; set; }
        }

        private class CurrentWeather
        {
            public double Temperature { get; set; }
            public double Windspeed { get; set; }
            public int Weathercode { get; set; }
        }

        private class DailyData
        {
            public string[]? Time { get; set; }
            public int[]? Weathercode { get; set; }
            public double[]? Temperature_2m_max { get; set; }
            public double[]? Temperature_2m_min { get; set; }
            public double[]? Precipitation_probability_max { get; set; }
            public double[]? Windspeed_10m_max { get; set; }
        }
    }
}