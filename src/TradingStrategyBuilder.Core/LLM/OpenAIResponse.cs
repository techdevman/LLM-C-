using Newtonsoft.Json;

namespace TradingStrategyBuilder.Core.LLM
{
    internal class OpenAIResponse
    {
        [JsonProperty("choices")]
        public Choice[]? Choices { get; set; }
    }

    internal class Choice
    {
        [JsonProperty("message")]
        public Message? Message { get; set; }
    }

    internal class Message
    {
        [JsonProperty("content")]
        public string? Content { get; set; }
    }
}
