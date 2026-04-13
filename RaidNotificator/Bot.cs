using System.Net.Http.Json;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaidNotificator.Components;
using RaidNotificator.DTOs;
using RaidNotificator.Infrastructure;

namespace RaidNotificator
{
    public class Bot : IBot
    {
        private Dictionary<ulong, RaidEvent> _events = new();
        
        private List<Subscriber>? _subscribers;
        
        private ServiceProvider? _serviceProvider;

        private readonly HttpClient _http = new();
        private readonly ILogger<Bot> _logger;
        private readonly IConfiguration _configuration;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IMongoClientBot _mongo;

        public Bot(ILogger<Bot> logger, IConfiguration configuration, IMongoClientBot mongo)
        {
            _subscribers = new List<Subscriber>
            {
                new Subscriber
                {
                    Id = 715055211865178213,
                }
            };
            
            _logger = logger;
            _configuration = configuration;
            _mongo = mongo;

            DiscordSocketConfig config = new()
            {
                GatewayIntents = GatewayIntents.All
            };
            
            _client = new DiscordSocketClient(config);
            _commands = new CommandService();
        }

        public async Task StartAsync(ServiceProvider services)
        {
            string discordToken = _configuration["DCToken"] ?? throw new Exception("Missing Discord token");
            
            _client.Log += message =>
            {
                _logger.LogInformation(message.Message);
                return Task.CompletedTask;
            };
            
            _logger.LogInformation($"Starting up with token {discordToken}");

            _serviceProvider = services;

            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
            
            _client.MessageUpdated += OnMessageUpdateAsync;
            _client.MessageReceived += OnMessageReceivedAsync;
            _client.GuildScheduledEventCreated += OnEventCreateAsync;
            _client.GuildScheduledEventUpdated += OnEventUpdateAsync;
            _client.Ready += OnReadyAsync;
            
            await _client.LoginAsync(TokenType.Bot, discordToken);
            await _client.StartAsync();
        }
        
        public async Task StopAsync()
        {
            _logger.LogInformation("Shutting down");
            
            await _client.LogoutAsync();
            await _client.StopAsync();

        }

        #region Handler

        private async Task OnMessageUpdateAsync(Cacheable<IMessage, ulong> before, SocketMessage after,
            ISocketMessageChannel channel)
        {
            var previous  = await _mongo.GetEventAsync(after.Id.ToString());
            if (previous == null)
            {
                _logger.LogInformation("No Event in db with ID:{AfterId}", after.Id);
                return;
            }
            
            try
            {
                var updated = await _http.GetFromJsonAsync<RaidEvent>($"https://raid-helper.xyz/api/v4/events/{after.Id}");

                if (updated == null)
                {
                    _logger.LogInformation("Raid-Helper API does not find event with ID:{MessageId}", after.Id);
                    return;
                }

                var diff = await DifferenceGenerator.GetRegistrationDiffAsync(previous, updated);

                if (diff != null)
                {
                    await _mongo.UpdateEventAsync(updated);
                    var message = await InfoMessage.UpdateRegistrationInfoAsync(diff, updated);
                    await NotifySubscribersAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private async Task OnMessageReceivedAsync(SocketMessage arg)
        {
            if (arg is not SocketUserMessage message)
            {
                return;
            }
            
            if (message.Author.Username.Contains("Raid-Helper"))
            {
                _logger.LogInformation(message.Type.ToString());
                var raidEvent = await _http.GetFromJsonAsync<RaidEvent>($"https://raid-helper.xyz/api/v4/events/{message.Id}");

                if (raidEvent == null)
                {
                    _logger.LogWarning($"Raid event with ID:{message.Id} not found!");
                    return;
                }
                
                _mongo.InsertEventAsync(raidEvent).GetAwaiter().GetResult();
            }
        }

        private Task OnEventUpdateAsync(Cacheable<SocketGuildEvent, ulong> old, SocketGuildEvent after)
        {
            _logger.LogInformation($"Functionality not implemented yet! Event with ID {after.Id} was updated!");
            return Task.CompletedTask;
        }

        private async Task OnEventCreateAsync(SocketGuildEvent newEvent)
        {
            var message = await InfoMessage.NewEventMessageAsync(newEvent);
            await NotifySubscribersAsync(message);
        }
        
        private async Task OnReadyAsync()
        {
            var guild = _client.GetGuild(ulong.Parse(_configuration["GuildId"]!));
            var channel = guild?.TextChannels
                .FirstOrDefault(c => c.Name == _configuration["ChannelName"]);

            if (channel == null)
            {
                _logger.LogWarning("Channel nicht gefunden");
                return;
            }

            var messages = await channel.GetMessagesAsync(10).FlattenAsync();
            foreach (var message in messages)
            {
                var updated = await _http.GetFromJsonAsync<RaidEvent>($"https://raid-helper.xyz/api/v4/events/{message.Id}");
                
                if (updated == null)
                    continue;
                
                _mongo.InsertEventAsync(updated).GetAwaiter().GetResult();
            }
        }
        #endregion

        private async Task NotifySubscribersAsync(ComponentBuilderV2 builder)
        {
            if (_subscribers == null)
            {
                _logger.LogInformation("No subscribers to notify");
                return;
            }
            
            foreach (var sub in _subscribers)
            {
                var dmChannel = await _client.GetUser(sub.Id).CreateDMChannelAsync();
                await dmChannel.SendMessageAsync(components: builder.Build());
            }
        }
    }
}