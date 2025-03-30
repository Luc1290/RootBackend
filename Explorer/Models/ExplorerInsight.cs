namespace RootBackend.Explorer.Models
{
    public class ExplorerInsight
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Topic { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Insight { get; set; } = "";
        public List<string>? Sources { get; set; }
    }
}
