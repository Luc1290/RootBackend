using System.Collections.Generic;

namespace RootBackend.Models
{
    public class NlpResponse
    {
        public string Intent { get; set; } = "autre";
        public Dictionary<string, string> Entities { get; set; } = new();
    }
}
