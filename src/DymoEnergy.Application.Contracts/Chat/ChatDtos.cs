using System.Collections.Generic;

namespace DymoEnergy.Chat;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;   // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
}

public class ChatRequest
{
    public List<ChatMessage> Messages { get; set; } = [];
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string? Error { get; set; }  // "quota_exceeded" | "unauthorized" | null
}
