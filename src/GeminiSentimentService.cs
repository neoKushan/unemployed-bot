// src/GeminiSentimentService.cs

using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Mscc.GenerativeAI;

public class GeminiSentimentService
{
    private readonly ILogger<GeminiSentimentService> _logger;
    private readonly IConfiguration _config;
    private readonly GenerativeModel? _generativeModelClient;
    private readonly string? _modelId;
    private readonly string? _apiKey;
    private readonly string _botPersonality;
    private readonly string _responseStyle;
    private readonly string _responseConstraints;
    private readonly int _maxContextMessages;
    private readonly bool _includeUsernames;
    private readonly bool _includeTimestamps;
    private readonly bool _isOperational = false;
    public bool IsOperational => _isOperational;

    public GeminiSentimentService(ILogger<GeminiSentimentService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        _modelId = _config["GeminiSettings:ModelId"];
        _apiKey = _config["GeminiSettings:ApiKey"];
        
        // Load prompt components
        _botPersonality = _config["GeminiSettings:PromptComponents:BotPersonality"] ?? 
            "You are a helpful and friendly Discord bot.";
        _responseStyle = _config["GeminiSettings:PromptComponents:ResponseStyle"] ?? 
            "Respond in a concise and engaging way.";
        _responseConstraints = _config["GeminiSettings:PromptComponents:ResponseConstraints"] ?? 
            "Keep your response under 200 characters.";

        // Load context settings
        _maxContextMessages = _config.GetValue<int>("GeminiSettings:ContextSettings:MaxContextMessages", 5);
        _includeUsernames = _config.GetValue<bool>("GeminiSettings:ContextSettings:IncludeUsernames", true);
        _includeTimestamps = _config.GetValue<bool>("GeminiSettings:ContextSettings:IncludeTimestamps", false);

        if (string.IsNullOrWhiteSpace(_modelId) || string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini settings (ApiKey, ModelId) are missing or incomplete in configuration. Bot responses will be disabled.");
            return;
        }

        try
        {
            _generativeModelClient = new GoogleAI(apiKey: _apiKey)
                                        .GenerativeModel(model: _modelId);

            _logger.LogInformation("Mscc.GenerativeAI client initialized successfully for model {ModelId}. Bot responses enabled.", _modelId);
            _isOperational = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Mscc.GenerativeAI client for model {ModelId}. Check configuration and API Key validity. Bot responses will be disabled.", _modelId);
            _generativeModelClient = null;
        }
    }

    public async Task<string> GetResponseAsync(SocketUserMessage message)
    {
        if (!_isOperational || _generativeModelClient == null)
        {
            _logger.LogWarning("Bot response skipped: Gemini service is not operational (check configuration and initialization logs).");
            return string.Empty;
        }

        var prompt = BuildPrompt(message);
        _logger.LogDebug("Generated prompt for Gemini: {Prompt}", prompt);

        var generationConfig = new GenerationConfig
        {
            Temperature = 0.7f,
            MaxOutputTokens = 1000,
            TopP = 0.8f,
            TopK = 40
        };

        try
        {
            _logger.LogDebug("Sending message to Gemini for response generation");

            var response = await _generativeModelClient.GenerateContent(prompt, generationConfig);

            if (response?.Candidates == null || !response.Candidates.Any())
            {
                _logger.LogWarning("Gemini returned no candidates in the response.");
                return string.Empty;
            }

            var candidate = response.Candidates.First();
            if (candidate?.Content?.Parts == null)
            {
                _logger.LogWarning("Gemini returned an empty or null response.");
                return string.Empty;
            }

            var parts = candidate.Content.Parts;
            if (parts == null)
            {
                _logger.LogWarning("Gemini returned null parts in the response.");
                return string.Empty;
            }

            // Join all parts of the response together
            var botResponse = string.Join(" ", parts
                .Select(part => part.Text?.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text)));

            if (string.IsNullOrWhiteSpace(botResponse))
            {
                _logger.LogWarning("Gemini returned an empty or null response after joining parts.");
                return string.Empty;
            }

            _logger.LogDebug("Received response from Gemini: {Response}", botResponse);
            return botResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini GenerateContentAsync for model {ModelId}", _modelId);
            return string.Empty;
        }
    }

    private string BuildPrompt(SocketUserMessage message)
    {
        var promptBuilder = new StringBuilder();

        // Add bot personality and instructions
        promptBuilder.AppendLine(_botPersonality);
        promptBuilder.AppendLine(_responseStyle);
        promptBuilder.AppendLine(_responseConstraints);
        promptBuilder.AppendLine();

        // Add context if available
        var contextMessages = GetContextMessages(message);
        if (contextMessages.Any())
        {
            promptBuilder.AppendLine("Previous messages in this conversation:");
            foreach (var contextMsg in contextMessages)
            {
                var timestamp = _includeTimestamps ? $" [{contextMsg.Timestamp:HH:mm}]" : "";
                var username = _includeUsernames ? $"{contextMsg.Author.Username}: " : "";
                promptBuilder.AppendLine($"{username}{contextMsg.Content}{timestamp}");
            }
            promptBuilder.AppendLine();
        }

        // Add the current message
        promptBuilder.AppendLine("Current message:");
        promptBuilder.AppendLine($"\"{message.Content}\"");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Response:");

        return promptBuilder.ToString();
    }

    private IEnumerable<SocketUserMessage> GetContextMessages(SocketUserMessage message)
    {
        var contextMessages = new List<SocketUserMessage>();
        
        // If the message is a reply, get the referenced message
        if (message.Reference?.MessageId.IsSpecified == true)
        {
            var referencedMessage = message.Channel.GetMessageAsync(message.Reference.MessageId.Value).Result as SocketUserMessage;
            if (referencedMessage != null)
            {
                contextMessages.Add(referencedMessage);
            }
        }

        // If in a thread, get previous messages
        if (message.Channel is IThreadChannel thread)
        {
            var messages = thread.GetMessagesAsync(_maxContextMessages).FlattenAsync().Result;
            contextMessages.AddRange(messages.OfType<SocketUserMessage>().Where(m => m.Id != message.Id));
        }

        return contextMessages.OrderBy(m => m.Timestamp);
    }
}
