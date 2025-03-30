namespace RootBackend.Explorer.Models
{
    public class WeatherResult
    {
        public required string City { get; set; }
        public double Temperature { get; set; }
        public double WindSpeed { get; set; }
        public required string Condition { get; set; }
        // Nouvelle propriété pour stocker les prévisions
        public List<DailyForecast> Forecasts { get; set; } = new List<DailyForecast>();
    }

    // Nouveau modèle pour les prévisions quotidiennes
    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public double MaxTemperature { get; set; }
        public double MinTemperature { get; set; }
        public double WindSpeed { get; set; }
        public required string Condition { get; set; }
        public double PrecipitationProbability { get; set; }
    }
}