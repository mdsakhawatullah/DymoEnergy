namespace DymoEnergy.Chat;

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.1-8b-instant";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
}
