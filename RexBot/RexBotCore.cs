using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Serialization;
using Discord;
using Discord.WebSocket;
using RexBot;

namespace Discord1Test
{
    public class RexBotCore
    {
        [Serializable]
        public struct Token
        {
            public string Name;
            public string Value;
            
            public Token( string name, string value )
            {
                Name = name;
                Value = value;
            }
        }

        [Serializable]
        public struct InfoCommand
        {
            public string Command;
            public string Response;
            public bool IsPublic;
            public bool ImageResponse;

            public InfoCommand( string command, string response, bool isPublic = true, bool imageResponse = false )
            {
                Command = command;
                Response = response;
                IsPublic = isPublic;
                ImageResponse = imageResponse;
            }
        }
        private static RexBotCore _instance;
        public static RexBotCore Instance => _instance ?? (_instance = new RexBotCore());

        private Random _random = new Random();

        public DiscordSocketClient RexxarClient;
        public DiscordSocketClient RexbotClient;

        private const string ASKING_RESPONSE = "It seems you're asking if you can ask a question, rexxar usually ignores these.\r\n" +
                                               "If you have a Space Engineers or SESE bug, please report it on the KSH forum.\r\n" +
                                               "If you have a question about how to use SESE, ask in the server admin text channel, one of the other users can help.\r\n" +
                                               "Questions about modding/scripting/programming are best asked in the appropriate channel on the KSH discord server.\r\n" +
                                               "Otherwise if you feel you urgently need rexxar's attention, send another message with as much detail of your problem as you can give.";

        private const string FIRST_RESPONSE = "I see this is your first message to rexxar.\r\n" +
                                              "If you have a Space Engineers or SESE bug, please report it on the KSH forum.\r\n" +
                                              "If you have a question about how to use SESE, ask in the server admin text channel, one of the other users can help.\r\n" +
                                              "Questions about modding/scripting/programming are best asked in the appropriate channel on the KSH discord server.\r\n" +
                                              "Otherwise if you feel you urgently need rexxar's attention, send another message with as much detail of your problem as you can give.";

        private const string INTRO_MSG = "bleep bloop bleep, this is rexxar's auto-respond bot.";
        private const string FIXIT_RESPONSE = "No:tm: :sunglasses:";

        public const long REXXAR_ID = 135116459675222016;
        public const long REXBOT_ID = 264301401801228289;

        private static readonly List<string> _statuses = new List<string>()
                                                         {
                                                             "Space Engineers",
                                                             "Medieval Engineers",
                                                             "Subnautica",
                                                             "Minecraft",
                                                             "Minecraft in Space",
                                                             "DAT ENGINEERING GAME",
                                                             "With Marek <3",
                                                             "With your heart",
                                                             "Gone Home",
                                                             "Miner Wars",
                                                             "Naval Engineers",
                                                             "GoodAI",
                                                         };

        private Timer _statusTimer = new Timer(20 * 60 * 1000);

        private List<InfoCommand> _infoCommands = new List<InfoCommand>();
        private List<IChatCommand> _chatCommands = new List<IChatCommand>();

        public List<InfoCommand> InfoCommands { get { return _infoCommands; } }
        public List<IChatCommand> ChatCommands { get { return _chatCommands; } }

        static void Main(string[] args) => Instance.Run().GetAwaiter().GetResult();

        public async Task Run()
        {
            Console.WriteLine("Initializing...");
            string filename = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "tokens.xml" );
            if ( !File.Exists( filename ) )
            {
                Console.WriteLine( "Tokens file not found!" );
                return;
            }
            List<Token> tokens;
            using ( StreamReader reader = new StreamReader( filename ) )
            {
                Console.WriteLine( "Reading tokens..." );
                XmlSerializer x = new XmlSerializer( typeof(List<Token>) );
                tokens = (List<Token>)x.Deserialize( reader );
                Console.WriteLine($"Found: {tokens.Count}.");
                if(tokens.Count==2)
                    Console.WriteLine("Ok.");
                else
                {
                    throw new FileLoadException("Incorrect number of tokens!");
                }
                reader.Close();
            }
            ScanAssemblyForCommands();
            LoadCommands();
            _statusTimer.Elapsed += async (sender, args) => await SetRandomStatus();
            _statusTimer.Start();
            await Login(tokens);
            await Task.Delay(-1);
        }

        async Task<bool> Login(List<Token> tokens )
        {
            Console.WriteLine("Authenticating...");
            try
            {
                if (RexxarClient == null)
                    RexxarClient = new DiscordSocketClient();
                await RexxarClient.LoginAsync( TokenType.User, tokens.First( t => t.Name == "rexxar" ).Value);
                await RexxarClient.ConnectAsync();
                RexxarClient.MessageReceived += RexxarMessageReceived;

                if(RexbotClient == null)
                    RexbotClient = new DiscordSocketClient();
                await RexbotClient.LoginAsync(TokenType.Bot, tokens.First(t => t.Name == "rexbot").Value);
                await RexbotClient.ConnectAsync();
                await SetRandomStatus();
                RexbotClient.MessageReceived += RexbotMessageReceived;

                Console.WriteLine("Ready.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid Login");
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }

        public void SaveCommands()
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InfoCommands.xml")))
            {
                XmlSerializer x = new XmlSerializer(typeof(List<InfoCommand>));
                x.Serialize(writer, _infoCommands);
                writer.Close();
            }
        }

        public void LoadCommands()
        {
            Console.WriteLine("Loading info commands...");
            using (StreamReader reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InfoCommands.xml")))
            {
                XmlSerializer x = new XmlSerializer(typeof(List<InfoCommand>));
                _infoCommands = (List<InfoCommand>)x.Deserialize(reader);
                reader.Close();
            }
                Console.WriteLine($"Found: {_infoCommands.Count}.");
                Console.WriteLine("Ok.");
        }

        private void ScanAssemblyForCommands()
        {
            Console.WriteLine("Loading chat commands...");
            var types = Assembly.GetCallingAssembly().DefinedTypes;
            foreach (var type in types)
            {
                
                if (type.ImplementedInterfaces.Contains(typeof(IChatCommand)))
                {
                    Console.WriteLine("Found: " + type.FullName);
                    _chatCommands.Add((IChatCommand)Activator.CreateInstance(type));
                }
            }
        }

        private async Task RexbotMessageReceived(SocketMessage message)
        {
            var channel = message.Channel;

            if (message.Author.Id == REXBOT_ID)
                return;
            
            if ( message.MentionedUsers.Any( u => u.Id == REXBOT_ID ) )
            {
                await channel.SendMessageAsync( $"{message.Author.Mention} What do you want?!" );
                return;
            }

            foreach (var command in _infoCommands)
            {
                if (message.Content.Equals(command.Command, StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine($"{DateTime.Now} [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    if (!command.IsPublic && message.Author.Id != REXXAR_ID)
                    {
                        await channel.SendMessageAsync($"{message.Author.Mention} You aren't allowed to use that command!");
                        break;
                    }
                    Console.WriteLine("Responding: " + command.Response );
                    if (!command.ImageResponse)
                        await channel.SendMessageAsync($"{message.Author.Mention} {command.Response}");
                    else
                        await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, command.Response));
                }
            }
            
            string messageLower = message.Content.ToLower();
            foreach (var command in _chatCommands)
            {
                if (messageLower.StartsWith(command.Command))
                {
                    Console.WriteLine($"{DateTime.Now} [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    if (!command.IsPublic && message.Author.Id != REXXAR_ID)
                    {
                        Console.WriteLine("Not Authorized");
                        await channel.SendMessageAsync($"{message.Author.Mention} You aren't allowed to use that command!");
                        break;
                    }
                    string response;
                    try
                    {
                        response = await command.Handle(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        await channel.SendMessageAsync($"Error processing message!```{ex}```");
                        break;
                    }
                    Console.WriteLine("Responding: " + response);
                    if (!string.IsNullOrEmpty(response))
                        await channel.SendMessageAsync($"{message.Author.Mention} {response}");
                }
            }

            if (message.MentionedUsers.Any(u => u.Id == REXXAR_ID))
            {
                if (Regex.IsMatch(message.Content, @"fix.*it", RegexOptions.IgnoreCase))
                {
                    Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    await channel.SendMessageAsync($"{message.Author.Mention} {FIXIT_RESPONSE}");
                }
            }
        }

        private async Task RexxarMessageReceived(SocketMessage message)
        {
            var channel = message.Channel;

            if (channel.Id == 269660270769471488)
            {
                var guild = ((SocketGuildChannel)channel).Guild;
                var kappa = guild.Emojis.First(e => e.Name.Contains("Kappa"));
                await ((SocketUserMessage)message).AddReactionAsync(kappa.Name);
            }

            if (message.Author.Id == RexxarClient.CurrentUser.Id)
            {
                return;
            }

            bool asking = Regex.IsMatch(message.Content, @"can.*ask.*question|have.*question", RegexOptions.IgnoreCase);
            if (!(channel is SocketGuildChannel))
            {
                var messages = await channel.GetMessagesAsync().Flatten();
                if (messages.Count() < 2 || asking)
                {
                    Console.WriteLine($"Recieved message from {message.Author.Username}. Asking: {(asking ? "true" : "false")} Responding...");
                    await channel.SendMessageAsync(INTRO_MSG);
                    await channel.SendMessageAsync(asking ? ASKING_RESPONSE : FIRST_RESPONSE);
                    return;
                }
            }
            if (message.MentionedUsers.Any(u => u.Id == RexxarClient.CurrentUser.Id) && asking)
            {
                Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    Console.WriteLine("Responding in DM");
                    var chan = await message.Author.CreateDMChannelAsync();
                    await chan.SendMessageAsync(INTRO_MSG);
                    await chan.SendMessageAsync(ASKING_RESPONSE);
                
            }
        }

        public async Task<string> SetRandomStatus()
        {
            string status = _statuses.RandomElement();
            Console.WriteLine($"Setting status to random entry: {status}");
            await RexbotClient.SetGame(status);
            return status;
        }
    }

    public static class Extensions
    {
        private static Random _random = new Random();
        public static string ServerName( this ISocketMessageChannel channel )
        {
            var guildChannel = channel as SocketGuildChannel;
            return guildChannel?.Guild.Name ?? "Private";
        }

        public static T RandomElement<T>(this List<T> input)
        {
            if (input.Count == 1)
                return input[0];
            if (input.Count == 0)
                return default(T);
            int index = _random.Next(0, input.Count - 1);
            return input[index];
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey> (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
