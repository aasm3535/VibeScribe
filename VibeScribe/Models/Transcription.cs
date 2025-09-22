using System;

namespace VibeScribe.Models
{
    public class Transcription
    {
        public string? Title { get; set; }
        public string? Text { get; set; }
        public string? AudioFilePath { get; set; }
        public DateTime Timestamp { get; set; }
    }
}