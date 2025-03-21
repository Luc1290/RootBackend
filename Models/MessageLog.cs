using System;
using System.ComponentModel.DataAnnotations;

namespace RootBackend.Models
{
    public class MessageLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Sender { get; set; } = string.Empty; // "user" ou "bot"

        [Required]
        public string Source { get; set; } = string.Empty; // "public" ou "admin"

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "text"; // "text", "image", "file", etc.

        public string? AttachmentUrl { get; set; } // null si texte seulement

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
