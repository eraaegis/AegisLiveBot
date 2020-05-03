using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using AegisLiveBot.DAL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AegisLiveBot.Core.Services;
using System.Linq;
using System.Reflection;
using AegisLiveBot.Core.Services.Streaming;
using AegisLiveBot.Web.Commands.Fun;
using DSharpPlus.Interactivity;
using AegisLiveBot.Web.Commands;

namespace AegisLiveBot.Web
{
    public class LiveBot
    {
        public DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        private readonly ConfigJson _configJson;
        private IServiceProvider _serviceProvider;
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();
        public LiveBot(IServiceCollection services)
        {
            var json = string.Empty;

            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = sr.ReadToEnd();

            _configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            var config = new DiscordConfiguration
            {
                Token = _configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            Client = new DiscordClient(config);

            Client.Ready += OnClientReady;

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(5)
            });

            var db = new DbService();
            services.AddSingleton(db);
            services.AddSingleton(Client);
            services.AddSingleton(_configJson);

            services.AddSingleton<ITwitchPollService, TwitchPollService>();

            _serviceProvider = services.BuildServiceProvider();

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { _configJson.Prefix },
                EnableDms = false,
                EnableMentionPrefix = true,
                DmHelp = true,
                IgnoreExtraArguments = true,
                Services = _serviceProvider
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.RegisterCommands<StreamingCommands>();
            Commands.RegisterCommands<RoastCommands>();
            Commands.RegisterCommands<GamesCommands>();
            Commands.RegisterCommands<SearchCommands>();

            Client.ConnectAsync();

            SetUpServices();
        }

        private Task OnClientReady(ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        private void SetUpServices()
        {
            var service = _serviceProvider.GetService<ITwitchPollService>();
            _services.TryAdd(typeof(ITwitchPollService), service);
        }
    }
}
