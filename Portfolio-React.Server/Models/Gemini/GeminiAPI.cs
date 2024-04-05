using System.Text.Json.Serialization;

namespace Portfolio_React.Server.Models.Gemini
{
    public class GeminiRequest
    {
        public List<MessagePart> contents { get; set; }
    }

    public class MessagePart
    {
        public string role { get; set; }
        public List<Part> parts { get; set; }
    }

    public class Part
    {
        public string text { get; set; }
    }

    public class GeminiResponse
    {
        public List<Candidate> candidates { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
        public string finishReason { get; set; }
        public int index { get; set; }
        public List<SafetyRating> safetyRatings { get; set; }
    }

    public class Content
    {
        public List<Part> parts { get; set; }
        public string role { get; set; }
    }

    public class SafetyRating
    {
        public string category { get; set; }
        public string probability { get; set; }
    }

    // Simple struct for storing message history
    public struct GeminiMessage
    {
        public string role { get; set; }
        public string text { get; set; }
        public GeminiMessage(string role, string text)
        {
            this.role = role;
            this.text = text;
        }
    }
}
