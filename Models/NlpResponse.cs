namespace RootBackend.Models
{
    public class NlpResponse
    {
        public string Intent { get; set; } = "autre";
        public double Confidence { get; set; } = 0.0;
    }
}