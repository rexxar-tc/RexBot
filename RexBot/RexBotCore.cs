using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord;
using Discord.Addons.EmojiTools;
using Discord.WebSocket;
using RexBot.Commands;
using Timer = System.Timers.Timer;

namespace RexBot
{
    public class RexBotCore
    {
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
        private static RexBotCore _instance;

        private static readonly List<string> _statuses = new List<string>
                                                         {
                                                             "Space Engineers",
                                                             "Medieval Engineers",
                                                             "Subnautica",
                                                             //"Minecraft in Space",
                                                             //"DAT ENGINEERING GAME",
                                                             "With Marek <3",
                                                             //"With your heart",
                                                             "Gone Home",
                                                             "Miner Wars",
                                                             "Naval Engineers",
                                                             "GoodAI",
                                                             "Factorio",
                                                             "Tomb Raider",
                                                             "Goat Simulator",
                                                             "Space Engineers 2",
                                                             "Oxygen Not Included",
                                                             "Event[0]",
                                                             "Space Colony",
                                                             "The Talos Principle",
                                                         };

        private static List<ulong> _bannedUsers = new List<ulong>();

        private Random _random = new Random();

        public Dictionary<ulong, BugreportBuilder> BugBuilders = new Dictionary<ulong, BugreportBuilder>();


        private readonly Timer _statusTimer = new Timer(20 * 60 * 1000);
        public JiraManager Jira;
        public Sheets PublicSheet;
        public DiscordSocketClient RexbotClient;

        public DiscordSocketClient RexxarClient;

        public SocketGuild KeenGuild;

        public Sheets CTGSheet;
        public static RexBotCore Instance => _instance ?? (_instance = new RexBotCore());

        public List<InfoCommand> InfoCommands { get; private set; } = new List<InfoCommand>();
        public List<IChatCommand> ChatCommands { get; } = new List<IChatCommand>();

        public List<ulong> BannedUsers
        {
            get { return _bannedUsers; }
        }

        public DBManager DBManager;

        private static void Main(string[] args) => Instance.Run().GetAwaiter().GetResult();

        public async Task Run()
        {
            try
            {
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Console.WriteLine("Initializing...");
                //Trace.Listeners.Clear();
                //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                Console.CancelKeyPress += Console_CancelKeyPress;
                List<Token> tokens = LoadTokens();
                ScanAssemblyForCommands();
                LoadCommands();
                _statusTimer.Elapsed += async (sender, args) => await SetRandomStatus();
                await Login(tokens);
                _statusTimer.Start();
                string ctgKey = tokens.First(t => t.Name == "CTGSheet").Value;
                string publicKey = tokens.First(t => t.Name == "PublicSheet").Value;
                CTGSheet = new Sheets(ctgKey);
                PublicSheet = new Sheets(publicKey);
                CommandBugReport.PublicList = publicKey;
                CommandBugReport.CTGList = ctgKey;
                try
                {
                    DBManager = new DBManager("KeenLog.db");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                LoadBanned();
                Console.WriteLine("Loading missed history");
                await GetMissingHistory();
                Console.WriteLine("Ready");
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled exception");
            Console.WriteLine(e.ExceptionObject);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
           Console.WriteLine("Unobserved exception");
            Console.WriteLine(e.Exception);
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            DBManager.Close();
        }

        private async Task<bool> Login(List<Token> tokens)
        {
            Console.WriteLine("Authenticating...");
            try
            {
                if (RexxarClient == null)
                    RexxarClient = new DiscordSocketClient();
                await RexxarClient.LoginAsync(TokenType.User, tokens.First(t => t.Name == "rexxar").Value);
                await RexxarClient.StartAsync();
                RexxarClient.MessageReceived += RexxarMessageReceived;

                if (RexbotClient == null)
                    RexbotClient = new DiscordSocketClient(new DiscordSocketConfig() {MessageCacheSize = 1000});
                 await RexbotClient.LoginAsync(TokenType.Bot, tokens.First(t => t.Name == "rexbot").Value);
                 await RexbotClient.StartAsync();

                AutoResetEvent e = new AutoResetEvent(false);
                RexbotClient.Ready += delegate
                                      {
                                          e.Set();
                                          return Task.CompletedTask;
                                      };

                Console.WriteLine("Waiting for fucking Volt");
                e.WaitOne();
                Console.WriteLine("Waiting done.");
                
                await SetRandomStatus();
                //await RexbotClient.SetGame( "Try !bugreport" );
                RexbotClient.MessageReceived += RexbotMessageReceived;
                RexbotClient.JoinedGuild += RexbotJoinedGuild;
                KeenGuild = RexbotClient.GetGuild(125011928711036928);
                
                Jira = new JiraManager(tokens.First(t => t.Name == "JiraURL").Value, "rex.bot", tokens.First(t => t.Name == "JiraPass").Value);
                
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

        private async Task RexbotJoinedGuild(SocketGuild arg)
        {
            Console.WriteLine($"Added to guild: '{arg.Name}'");
            if (!arg.Users.Any(u => u.Id == REXXAR_ID))
            {
                Console.WriteLine("Rexxar not in this guild. Leaving.");
                var chan = arg.DefaultChannel;
                await chan.SendMessageAsync("You are not authorized to use this bot!");
                await arg.LeaveAsync();
            }
        }

        public void SaveCommands()
        {
            using (var writer = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InfoCommands.xml")))
            {
                var x = new XmlSerializer(typeof(List<InfoCommand>));
                x.Serialize(writer, InfoCommands);
                writer.Close();
            }
        }

        public void LoadCommands()
        {
            string filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InfoCommands.xml");
            if (!File.Exists(filename))
            {
                Console.WriteLine("InfoCommands file not found!");
                InfoCommands = new List<InfoCommand>();
                return;
            }
            Console.WriteLine("Loading info commands...");
            using (var reader = new StreamReader(filename))
            {
                var x = new XmlSerializer(typeof(List<InfoCommand>));
                InfoCommands = (List<InfoCommand>)x.Deserialize(reader);
                reader.Close();
            }

            Console.WriteLine($"Found: {InfoCommands.Count}.");
            Console.WriteLine("Ok.");
        }

        public List<Token> LoadTokens()
        {
            string filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tokens.xml");
            if (!File.Exists(filename))
            {
                Console.WriteLine("Tokens file not found!");
                return null;
            }
            List<Token> tokens;
            using (var reader = new StreamReader(filename))
            {
                Console.WriteLine("Reading tokens...");
                var x = new XmlSerializer(typeof(List<Token>));
                tokens = (List<Token>)x.Deserialize(reader);
                Console.WriteLine($"Found: {tokens.Count}.");
                if (tokens.Count >= 2)
                    Console.WriteLine("Ok.");
                else
                    throw new FileLoadException("Incorrect number of tokens!");
                reader.Close();
            }

            return tokens;
        }

        public void LoadBanned()
        {
            string filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BannedUsers.xml");
            if (!File.Exists(filename))
            {
                Console.WriteLine("Banned users file not found.");
                _bannedUsers = new List<ulong>();
                return;
            }

            Console.WriteLine("Loading banned users...");
            using (var reader = new StreamReader(filename))
            {
                var x = new XmlSerializer(typeof(List<ulong>));
                _bannedUsers = (List<ulong>)x.Deserialize(reader);
                reader.Close();
            }

            Console.WriteLine($"Found: {_bannedUsers.Count}.");
            Console.WriteLine("Ok.");
        }

        public void SaveBanned()
        {
            using (var writer = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BannedUsers.xml")))
            {
                var x = new XmlSerializer(typeof(List<ulong>));
                x.Serialize(writer, _bannedUsers);
                writer.Close();
            }
        }

        private void ScanAssemblyForCommands()
        {
            try
            {
                Console.WriteLine("Loading chat commands...");
                var assembly = Assembly.GetEntryAssembly();
                Console.WriteLine($"Loading {assembly.FullName}");
                var types = GetTypesSafely(assembly);
                foreach (TypeInfo type in types)
                {
                    try
                    {
                        if (type.ImplementedInterfaces.Contains(typeof(IChatCommand)))
                        {
                            Console.WriteLine("Found: " + type.FullName);
                            ChatCommands.Add((IChatCommand)Activator.CreateInstance(type));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        var l = ex as ReflectionTypeLoadException;
                        if (l != null)
                            Console.WriteLine(string.Join<Exception>("\r\n", l.LoaderExceptions));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                var l = ex as ReflectionTypeLoadException;
                if(l!=null)
                    Console.WriteLine(string.Join<Exception>("\r\n",l.LoaderExceptions));
            }
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }

        private async Task RexbotMessageReceived(SocketMessage message)
        {
            ISocketMessageChannel channel = message.Channel;

            if (message.Author.Id == REXBOT_ID || message.Author.Id == 186606257317085184)
                return;
            
            if (_bannedUsers.Contains(message.Author.Id))
            {
                //Console.WriteLine($"Responding to banned user {message.Author.Username}");
                //await channel.SendMessageAsync($"{message.Author.Mention} You've been banned from using RexBot.");
                return;
            }
            
            if (message.Author.IsBot)
                return;

            BugreportBuilder builder;
            if (BugBuilders.TryGetValue(channel.Id, out builder))
            {
                await builder.Process(message);
                if (builder.CurrentStep == BugreportBuilder.StepEnum.Finished)
                    BugBuilders.Remove(channel.Id);
                return;
            }

            //if ( message.MentionedUsers.Any( u => u.Id == REXBOT_ID ) )
            //{
            //    await channel.SendMessageAsync( $"{message.Author.Mention} What do you want?!" );
            //    return;
            //}

            foreach (InfoCommand command in InfoCommands)
                if (message.Content.Equals(command.Command, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (_bannedUsers.Contains(message.Author.Id))
                    {
                        Console.WriteLine($"Responding to banned user {message.Author.Username}");
                        await channel.SendMessageAsync($"{message.Author.Mention} You've been banned from using RexBot.");
                        return;
                    }

                    Console.WriteLine($"{DateTime.Now} [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    if (!command.IsPublic && (message.Author.Id != REXXAR_ID))
                    {
                        await channel.SendMessageAsync($"{message.Author.Mention} You aren't allowed to use that command!");
                        break;
                    }
                    Console.WriteLine("Responding: " + command.Response);
                    if (!command.ImageResponse)
                        await channel.SendMessageAsync($"{message.Author.Mention} {command.Response}");
                    else
                    {
                        if (!command.Response.StartsWith("http"))
                            await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, command.Response), message.Author.Mention);
                        else
                        {
                            EmbedBuilder em = new EmbedBuilder();
                            em.ImageUrl = command.Response;
                            await channel.SendMessageAsync(message.Author.Mention, embed: em);
                        }
                    }
                }

            string messageLower = message.Content.ToLower();
            foreach (IChatCommand command in ChatCommands)
                if (messageLower.StartsWith(command.Command))
                {
                    if (_bannedUsers.Contains(message.Author.Id))
                    {
                        Console.WriteLine($"Responding to banned user {message.Author.Username}");
                        await channel.SendMessageAsync($"{message.Author.Mention} You've been banned from using RexBot.");
                        return;
                    }

                    Console.WriteLine($"{DateTime.Now} [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    if (!command.HasAccess(message.Author))
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

            if (_bannedUsers.Contains(message.Author.Id))
            {
                //Console.WriteLine($"Responding to banned user {message.Author.Username}");
                //await channel.SendMessageAsync($"{message.Author.Mention} You've been banned from using RexBot.");
                return;
            }

            if (Regex.IsMatch(message.Content, @"water in SE|water in space engineers", RegexOptions.IgnoreCase))
            {
                await channel.SendMessageAsync(message.Author.Mention + " There are no plans to implement (liquid) water of any kind in Space Engineers.");
            }
            if (Regex.IsMatch(message.Content, @"water in ME|water in medieval engineers", RegexOptions.IgnoreCase))
            {
                await channel.SendMessageAsync(message.Author.Mention + " There are no plans to implement water of any kind in Medieval Engineers.");
            }
            //if (message.MentionedUsers.Any(u => u.Id == REXXAR_ID))
            //{
            //    if (Regex.IsMatch(message.Content, @"fix.*it", RegexOptions.IgnoreCase))
            //    {
            //        Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
            //        await channel.SendMessageAsync($"{message.Author.Mention} {FIXIT_RESPONSE}");
            //    }
            //}
        }

        private async Task RexxarMessageReceived(SocketMessage message)
        {
            ISocketMessageChannel channel = message.Channel;

            //if (channel.Id == 269660270769471488)
            //{
            //    await ((SocketUserMessage)message).AddReactionAsync(":xocKappa:269884941502775306");
            //}

            if (message.Author.Id == RexxarClient.CurrentUser.Id)
                return;

            bool asking = Regex.IsMatch(message.Content, @"can.*ask.*question|have.*question", RegexOptions.IgnoreCase);
            if (!(channel is SocketGuildChannel))
            {
                IEnumerable<IMessage> messages = await channel.GetMessagesAsync().Flatten();
                int count = messages.Count();
                if (((count < 2) || asking) && (count < 20))
                {
                    Console.WriteLine($"Recieved message from {message.Author.Username}. Asking: {(asking ? "true" : "false")} Responding...");
                    await channel.SendMessageAsync(INTRO_MSG);
                    await channel.SendMessageAsync(asking ? ASKING_RESPONSE : FIRST_RESPONSE);
                }
            }


            //if (message.MentionedUsers.Any(u => u.Id == RexxarClient.CurrentUser.Id) && asking)
            //{
            //    Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
            //        Console.WriteLine("Responding in DM");
            //        var chan = await message.Author.CreateDMChannelAsync();
            //        await chan.SendMessageAsync(INTRO_MSG);
            //        await chan.SendMessageAsync(ASKING_RESPONSE);

            //}
        }

        public string GetRandomEmoji()
        {
            return $":{EmojiMap.Map.RandomElement().Key}:";
        }


        Queue<string> Last5Status = new Queue<string>(5);
        public async Task<string> SetRandomStatus()
        {
            string status;
            while (true)
            {
                status = _statuses.RandomElement();
                if (Last5Status.Contains(status))
                {
                    Console.WriteLine($"Got duplicate status: {status}");
                    continue;
                }
                break;
            }

            if (Last5Status.Count >= 5)
                Last5Status.Dequeue();
            Last5Status.Enqueue(status);

            Console.WriteLine($"Setting status to random entry: {status}");
            await RexbotClient.SetGameAsync(status);
            return status;
        }

        public async Task GetMissingHistory()
        {
            long count = 0;
            bool updating = true;
            long minDate = long.MaxValue;
            long minDateLocal = long.MaxValue;
            List<IMessage> _messages = new List<IMessage>();
            ISocketMessageChannel _currentChannel = null;

            SocketGuild server = Instance.KeenGuild;
            
            var channels = server.TextChannels;

            var compareTime = TimeSpan.FromHours(1);

            foreach (var c in channels)
            {
                try
                {
                    var channel = c as ISocketMessageChannel;
                    if (channel == null)
                        continue;

                    Console.WriteLine($"Switching to {channel.Name}");

                    var users = await channel.GetUsersAsync().Flatten();
                    if (!users.Any(u => u.Id == RexBotCore.REXBOT_ID))
                    {
                        Console.WriteLine("No permission here :(");
                        continue;
                    }
                    
                    var tmp = (await channel.GetMessagesAsync().Flatten()).ToList();
                   
                    while (true)
                    {
                        if (!tmp.Any())
                            break;
                        foreach (var msg in tmp)
                        {
                            //RexBotCore.Instance.DBManager.AddMessage(msg);
                            lock (_messages)
                                _messages.Add(msg);
                            count++;
                            if (DateTime.UtcNow - msg.Timestamp.UtcDateTime > compareTime)
                                break;
                        }

                        tmp = (await channel.GetMessagesAsync(tmp.First(m => m.Timestamp.UtcTicks == tmp.Min(n => n.Timestamp.UtcTicks)).Id, Direction.Before).Flatten()).ToList();
                        if (!tmp.Any())
                            break;
                        //Thread.Sleep(500);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            Console.WriteLine($"Found {_messages.Count} messages");
            RexBotCore.Instance.DBManager.AddMessages(_messages);
        }

        [Serializable]
        public struct Token
        {
            public string Name;
            public string Value;

            public Token(string name, string value)
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
            public ulong Author;

            public InfoCommand(string command, string response, ulong author, bool isPublic = true, bool imageResponse = false)
            {
                Command = command;
                Response = response;
                Author = author;
                IsPublic = isPublic;
                ImageResponse = imageResponse;
            }
        }
    }
}