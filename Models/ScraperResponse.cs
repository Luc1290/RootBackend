namespace RootBackend.Models
{
    public class ScraperResponse
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string Content { get; set; } = "";
        public string FullPageContent { get; set; } = "";
    }
}
