// src/GeminiSentimentService.cs

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Mscc.GenerativeAI; // Use the library's namespace

public class GeminiSentimentService
{
    private readonly ILogger<GeminiSentimentService> _logger;
    private readonly IConfiguration _config;
    private readonly GenerativeModel? _generativeModelClient;
    private readonly string? _modelId;
    private readonly string? _apiKey;
    private readonly string _promptTemplate;
    private readonly bool _isOperational = false;
    public bool IsOperational => _isOperational;

    public GeminiSentimentService(ILogger<GeminiSentimentService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        _modelId = _config["GeminiSettings:ModelId"];
        _apiKey = _config["GeminiSettings:ApiKey"];
        _promptTemplate = _config["GeminiSettings:PromptTemplate"] ?? 
            "You are a helpful and friendly Discord bot. Respond to the following message in a concise and engaging way. Keep your response under 200 characters.\n\nMessage:\n\"{0}\"\n\nResponse:";

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

    public async Task<string> GetResponseAsync(string messageContent)
    {
        if (!_isOperational || _generativeModelClient == null)
        {
            _logger.LogWarning("Bot response skipped: Gemini service is not operational (check configuration and initialization logs).");
            return string.Empty;
        }

        var prompt = string.Format(_promptTemplate, messageContent);

        var generationConfig = new GenerationConfig
        {
            Temperature = 0.7f,
            MaxOutputTokens = 100,
            TopP = 0.8f,
            TopK = 40
        };

        try
        {
            _logger.LogDebug("Sending message to Gemini for response generation");

            var response = await _generativeModelClient.GenerateContent(prompt, generationConfig);

            string? botResponse = response?.Candidates?.FirstOrDefault()?
                                        .Content?.Parts?.FirstOrDefault()?
                                        .Text?.Trim();

            if (string.IsNullOrWhiteSpace(botResponse))
            {
                _logger.LogWarning("Gemini returned an empty or null response.");
                return string.Empty;
            }

            return botResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini GenerateContentAsync for model {ModelId}", _modelId);
            return string.Empty;
        }
    }
}
