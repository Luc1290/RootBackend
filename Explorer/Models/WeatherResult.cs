namespace RootBackend.Explorer.Models
{
    public class WeatherResult
    {
        public string City { get; set; } = "";
        public double Temperature { get; set; }
        public double WindSpeed { get; set; }
        public string Condition { get; set; } = "";
    }
}
