// src/Program.cs
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                // Load the appsettings.json
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                // Load environment-specific settings (e.g., appsettings.Development.json)
                config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                // Load user secrets if in Development environment (for API keys etc.)
                if (hostContext.HostingEnvironment.IsDevelopment())
                {
                    config.AddUserSecrets<Program>(); // Assumes Program is in your main assembly
                }
                // Load environment variables (often used for production secrets)
                config.AddEnvironmentVariables();
            })
            .ConfigureLogging(logging =>
            {
                // Logging configuration is often handled by CreateDefaultBuilder reading
                // the "Logging" section from appsettings.json.
                // You can customize it further here if needed.
                logging.ClearProviders(); // Optional: Remove default providers like EventLog
                logging.AddConsole();
                // Add other providers like Debug, File, etc.
            })
            .ConfigureServices((hostContext, services) =>
            {
                // --- Configure Discord Client ---
                var clientConfig = new DiscordSocketConfig
                {
                    // Specify necessary Gateway Intents
                    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent,
                    // Consider setting LogLevel here if desired, or rely on LogAsync mapping
                    // LogLevel = LogSeverity.Info
                };
                // Register DiscordSocketClient as a singleton
                services.AddSingleton(new DiscordSocketClient(clientConfig));

                // Add Gemini stuff
                services.AddSingleton<GeminiSentimentService>();

                // --- Register Custom Services ---
                // Register MessageHandlerService (scoped or transient might be ok too, but singleton is simple)
                services.AddSingleton<MessageHandlerService>();

                // Register DiscordBotService as a Hosted Service
                // This ensures its StartAsync and StopAsync methods are called by the host
                services.AddHostedService<DiscordBotService>();

                // --- Optional: Register configuration options pattern if needed ---
                // services.Configure<MySettings>(hostContext.Configuration.GetSection("MySettingsSection"));
            });
}
