namespace RootBackend.Explorer.Models
{
    public class WeatherResult
    {
        public required string City { get; set; }
        public double Temperature { get; set; }
        public double WindSpeed { get; set; }
        public required string Condition { get; set; }
        // Nouvelle propri�t� pour stocker les pr�visions
        public List<DailyForecast> Forecasts { get; set; } = new List<DailyForecast>();
    }

    // Nouveau mod�le pour les pr�visions quotidiennes
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