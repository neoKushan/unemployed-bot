using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class DiscordBotService : IHostedService
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IConfiguration _config;
    private readonly DiscordSocketClient _client;
    private readonly MessageHandlerService _messageHandler;

    public DiscordBotService(
        ILogger<DiscordBotService> logger,
        IConfiguration config,
        DiscordSocketClient client,
        MessageHandlerService messageHandler)
    {
        _logger = logger;
        _config = config;
        _client = client;
        _messageHandler = messageHandler;

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += _messageHandler.HandleMessageAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Discord Bot Service...");

        string? token = _config["DiscordBotToken"];
        if (string.IsNullOrWhiteSpace(token) || token == "YOUR_DISCORD_BOT_TOKEN_HERE")
        {
            _logger.LogCritical("Discord Bot Token is missing or invalid in configuration. Please update it. Service will not start.");
            throw new InvalidOperationException("Discord Bot Token is missing or invalid.");
        }

        try
        {
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            _logger.LogInformation("Bot connection initiated.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred during bot login or startup.");
            Environment.Exit(1);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Discord Bot Service...");
        await _client.StopAsync();
        await _client.LogoutAsync();
        _logger.LogInformation("Bot stopped and logged out.");
    }

    private async Task ReadyAsync()
    {
        _logger.LogInformation("{CurrentUser} is connected and ready!", _client.CurrentUser);
    }

    private Task LogAsync(LogMessage log)
    {
        LogLevel logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, log.Exception, "[Discord.Net] {Source}: {Message}", log.Source, log.Message);
        return Task.CompletedTask;
    }
}
