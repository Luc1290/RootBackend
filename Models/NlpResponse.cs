namespace RootBackend.Models
{
    public class NlpResponse
    {
        public string Intention { get; set; } = "";
        public List<string> Entities { get; set; } = new();
        public string SearchQuery { get; set; } = "";
    }
}
