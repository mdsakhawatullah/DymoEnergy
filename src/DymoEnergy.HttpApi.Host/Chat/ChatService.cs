using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.Chat;

public class ChatService : IChatService, ITransientDependency
{
    private const int MaxRetries = 2;

    private const string SystemPrompt =
        "You are a friendly solar energy assistant for Dymo Energy, Bangladesh's leading solar solutions provider. " +
        "You help customers with questions about solar panels, inverters, battery storage, load calculations, " +
        "system sizing, installation, warranties, and energy savings. " +
        "CRITICAL LANGUAGE RULE: Detect the language of the user's LAST message and reply ONLY in that exact same language. " +
        "If the user writes in English → reply in English only. " +
        "If the user writes in Bengali (বাংলা) → reply in Bengali only. " +
        "If the user writes in Chinese (中文) → reply in Chinese only. " +
        "NEVER switch languages. NEVER mix languages in a single reply. " +
        "Keep answers concise, helpful, and specific to solar energy topics. " +
        "If a question is unrelated to solar energy or Dymo Energy products, politely redirect the conversation back to solar topics. " +
        "When relevant, suggest the user visit /solar-calculator for sizing estimates or /quote to speak with an engineer.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAISettings _settings;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAISettings> options,
        ILogger<ChatService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request)
    {
        // Keep only the last 10 exchanges to stay within token limits
        var trimmedMessages = request.Messages.Count > 20
            ? request.Messages.GetRange(request.Messages.Count - 20, 20)
            : request.Messages;

        var messages = new List<object>
        {
            new { role = "system", content = SystemPrompt }
        };

        foreach (var msg in trimmedMessages)
            messages.Add(new { role = msg.Role, content = msg.Content });

        var payload = new
        {
            model = _settings.Model,
            messages,
            max_tokens = 1500,
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(payload);

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(30);

                var response = await client.PostAsync(_settings.BaseUrl, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API error {Status}: {Body}", (int)response.StatusCode, errorBody);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        // Parse OpenAI error to distinguish quota vs rate limit
                        var isQuotaExceeded = errorBody.Contains("insufficient_quota") || errorBody.Contains("exceeded your current quota");

                        if (isQuotaExceeded)
                        {
                            return new ChatResponse
                            {
                                Reply = "The AI service is currently unavailable (account quota exceeded). Please contact Dymo Energy for assistance, or visit /quote to speak with our team directly.",
                                Error = "quota_exceeded"
                            };
                        }

                        // Genuine rate limit — retry with backoff
                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                        if (attempt < MaxRetries)
                        {
                            _logger.LogWarning("OpenAI rate limited. Retrying after {Delay}s", retryAfter.TotalSeconds);
                            await Task.Delay(retryAfter);
                            continue;
                        }

                        return new ChatResponse
                        {
                            Reply = "I'm receiving a lot of requests right now. Please wait a moment and try again. ☀️"
                        };
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return new ChatResponse
                        {
                            Reply = "AI service configuration error (invalid API key). Please contact the site administrator.",
                            Error = "unauthorized"
                        };
                    }

                    return new ChatResponse
                    {
                        Reply = "Sorry, I couldn't get a response right now. Please try again in a moment."
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                var reply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                return new ChatResponse { Reply = reply.Trim() };
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("OpenAI request timed out (attempt {Attempt})", attempt + 1);
                if (attempt == MaxRetries)
                    return new ChatResponse { Reply = "The request timed out. Please try again." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenAI (attempt {Attempt})", attempt + 1);
                if (attempt == MaxRetries)
                    return new ChatResponse { Reply = "Something went wrong. Please try again later." };
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)));
        }

        return new ChatResponse { Reply = "Unable to connect to the AI service. Please try again later." };
    }
}
