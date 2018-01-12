using System;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PumpAndDumpBot.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.Interactive;

namespace PumpAndDumpBot
{
    class Program
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _services;

        public static readonly char COMMAND_PREFIX = '!';
        public static readonly Color EMBED_COLOR = Color.Red;

        public static void Main(string[] args)
            => new Program().StartAsync(args).GetAwaiter().GetResult();

        private async Task StartAsync(params string[] args)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            _commands = new CommandService();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            var _services = InstallServices();
            _services.GetRequiredService<CommandHandler>();
            _services.GetRequiredService<UserHandler>();

            _client.Log += Log;
            _commands.Log += Log;

            await _client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["TOKEN"]);
            await _client.StartAsync();

            await _client.SetGameAsync(ConfigurationManager.AppSettings["GAME"]);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        private IServiceProvider InstallServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<CommandHandler>()
                .AddSingleton<UserHandler>()
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(string.Concat("[", DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss"), "] [", msg.Severity, "] ", msg.Message, msg.Exception));
            return Task.CompletedTask;
        }
    }
}