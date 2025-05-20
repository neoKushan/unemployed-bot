// src/MessageHandlerService.cs
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class MessageHandlerService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<MessageHandlerService> _logger;
    private readonly GeminiSentimentService _geminiService;

    public MessageHandlerService(
        DiscordSocketClient client,
        ILogger<MessageHandlerService> logger,
        IConfiguration config,
        GeminiSentimentService geminiService)
    {
        _client = client;
        _logger = logger;
        _geminiService = geminiService;
        _logger.LogInformation("MessageHandlerService initialized.");
    }

    public async Task HandleMessageAsync(SocketMessage message)
    {
        // Ignore system messages or messages from other bots
        if (message is not SocketUserMessage userMessage || message.Author.IsBot)
        {
            return;
        }

        // Check if the bot is mentioned
        if (userMessage.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id))
        {
            await HandleMentionAsync(userMessage);
        }
    }

    private async Task HandleMentionAsync(SocketUserMessage message)
    {
        try
        {
            var response = await _geminiService.GetResponseAsync(message.Content);
            if (!string.IsNullOrWhiteSpace(response))
            {
                await message.Channel.SendMessageAsync(response);
                _logger.LogInformation("Sent Gemini response to channel {ChannelId}", message.Channel.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Gemini response for message in channel {ChannelId}", message.Channel.Id);
        }
    }
}
