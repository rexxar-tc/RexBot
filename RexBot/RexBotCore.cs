using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord.Addons.EmojiTools;
using DSharpPlus;
using DSharpPlus.Entities;
using RexBot.AutoCommands;
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

        private static HashSet<ulong> _bannedUsers = new HashSet<ulong>();
        private static Dictionary<string, Dictionary<ulong, bool>> _permissionOverrides;

        private Random _random = new Random();

        public Dictionary<ulong, BugreportBuilder> BugBuilders = new Dictionary<ulong, BugreportBuilder>();


        private readonly Timer _statusTimer = new Timer(20 * 60 * 1000);
        public JiraManager Jira;
        public Sheets PublicSheet;
        public TrelloManager Trello;
        public DiscordClient RexbotClient;

        public DiscordClient RexxarClient;

        public SteamWebApi.SteamWebApi SteamWebApi;

        public DiscordGuild KeenGuild;

        public Sheets CTGSheet;
        public static RexBotCore Instance => _instance ?? (_instance = new RexBotCore());

        public List<InfoCommand> InfoCommands { get; private set; } = new List<InfoCommand>();
        public List<AutoCommand> AutoCommands { get; private set; } = new List<AutoCommand>();
        public List<IChatCommand> ChatCommands { get; } = new List<IChatCommand>();
        public List<IAutoCommand> SystemAuto { get; } = new List<IAutoCommand>();
    
        public HashSet<ulong> BannedUsers => _bannedUsers;
        public Dictionary<string, Dictionary<ulong, bool>> PermissionOverrides => _permissionOverrides;

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
                var tokens = LoadTokens();
                ScanAssemblyForCommands();
                LoadCommands();
                _statusTimer.Elapsed += async (sender, args) => await SetRandomStatus();
                await Login(tokens);
                _statusTimer.Start();
                string ctgKey = tokens["CTGSheet"];
                string publicKey = tokens["PublicSheet"];
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
                LoadOverrides();

                SteamWebApi = new SteamWebApi.SteamWebApi(tokens["steamwebapi"]);
#if !DEBUG
                Console.WriteLine("Loading missed history");
                await GetMissingHistory();
#else
                await RexbotClient.UpdateStatusAsync(null, UserStatus.Offline);
#endif
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

        private async Task<bool> Login(Dictionary<string, string> tokens)
        {
            Console.WriteLine("Authenticating...");
            try
            {
//#if !DEBUG
//                if (RexxarClient == null)
//                    RexxarClient = new DiscordSocketClient();
//                await RexxarClient.LoginAsync(TokenType.User, tokens["rexxar"]);
//                await RexxarClient.StartAsync();
//                RexxarClient.MessageReceived += RexxarMessageReceived;
//#endif
                //if (RexbotClient == null)
                    RexbotClient = new DiscordClient(new DiscordConfiguration()
                                                     {
                                                         MessageCacheSize = 1000,
                                                         Token = tokens["rexbot"],
                                                         TokenType = TokenType.Bot
                                                     });
                await RexbotClient.ConnectAsync();

                AutoResetEvent e = new AutoResetEvent(false);
                RexbotClient.Ready += delegate
                                      {
                                          e.Set();
                                          return Task.CompletedTask;
                                      };

                Console.WriteLine("Waiting for ready");
                e.WaitOne();
                Console.WriteLine("Waiting done.");

#if !DEBUG
                //await RexxarClient.SetStatusAsync(UserStatus.Invisible);
                await SetRandomStatus();
                //await RexbotClient.SetGame( "Try !bugreport" );
#endif
                RexbotClient.MessageCreated += RexbotClient_MessageCreated;
                RexbotClient.GuildCreated += RexbotClient_GuildCreated;
                KeenGuild = await RexbotClient.GetGuildAsync(125011928711036928);
                Utilities.CTGChannels = new HashSet<DiscordChannel>();
                foreach(var id in Utilities.CTGChannelIds)
                    Utilities.CTGChannels.Add(await RexbotClient.GetChannelAsync(id));
                
                Jira = new JiraManager(tokens["JiraURL"], "rex.bot", tokens["JiraPass"]);
                Trello = new TrelloManager(tokens["TrelloKey"], tokens["TrelloToken"], tokens["TrelloPublic"], tokens["TrelloCTG"]);
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


        private async Task RexbotClient_GuildCreated(DSharpPlus.EventArgs.GuildCreateEventArgs e)
        {
            var arg = e.Guild;
            Console.WriteLine($"Added to guild: '{arg.Name}'");
            if (!arg.Members.Any(u => u.Id == REXXAR_ID))
            {
                Console.WriteLine("Rexxar not in this guild. Leaving.");
                var chan = arg.GetDefaultChannel();
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
            using (var writer = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoCommands.xml")))
            {
                var x = new XmlSerializer(typeof(List<AutoCommand>));
                x.Serialize(writer, AutoCommands);
                writer.Close();
            }
        }

        public void LoadCommands()
        {
            string FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InfoCommands.xml");
            if (!File.Exists(FileName))
            {
                Console.WriteLine("InfoCommands file not found!");
                InfoCommands = new List<InfoCommand>();
                return;
            }
            Console.WriteLine("Loading info commands...");
            using (var reader = new StreamReader(FileName))
            {
                var x = new XmlSerializer(typeof(List<InfoCommand>));
                InfoCommands = (List<InfoCommand>)x.Deserialize(reader);
                reader.Close();
            }

            Console.WriteLine($"Found: {InfoCommands.Count}.");

            FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoCommands.xml");
            if (!File.Exists(FileName))
            {
                Console.WriteLine("AutoCommands file not found!");
                AutoCommands = new List<AutoCommand>();
                return;
            }
            Console.WriteLine("Loading auto commands...");
            using (var reader = new StreamReader(FileName))
            {
                var x = new XmlSerializer(typeof(List<AutoCommand>));
                AutoCommands = (List<AutoCommand>)x.Deserialize(reader);
                reader.Close();
            }

            Console.WriteLine($"Found: {AutoCommands.Count}.");
            Console.WriteLine("Ok.");
        }

        public Dictionary<string, string> LoadTokens()
        {
            string FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tokens.xml");
            if (!File.Exists(FileName))
            {
                Console.WriteLine("Tokens file not found!");
                return null;
            }
            List<Token> tokens;
            using (var reader = new StreamReader(FileName))
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

            return tokens.ToDictionary(t => t.Name, t => t.Value);
        }

        public void LoadBanned()
        {
            string FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BannedUsers.xml");
            if (!File.Exists(FileName))
            {
                Console.WriteLine("Banned users file not found.");
                _bannedUsers = new HashSet<ulong>();
                return;
            }

            Console.WriteLine("Loading banned users...");
            using (var reader = new StreamReader(FileName))
            {
                var x = new XmlSerializer(typeof(List<ulong>));
                _bannedUsers = new HashSet<ulong>((List<ulong>)x.Deserialize(reader));
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
                x.Serialize(writer, _bannedUsers.ToList());
                writer.Close();
            }
        }

        public void LoadOverrides()
        {
            string FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PermissionOverride.xml");
            if (!File.Exists(FileName))
            {
                Console.WriteLine("Permission override file not found.");
                _permissionOverrides = new Dictionary<string, Dictionary<ulong, bool>>();
                return;
            }

            Console.WriteLine("Loading permission overrides...");
            using (var reader = new StreamReader(FileName))
            {
                int count=0;
                var x = new XmlSerializer(typeof(List<CommandOverride>));
                var raw = (List<CommandOverride>)x.Deserialize(reader);
                _permissionOverrides = new Dictionary<string, Dictionary<ulong, bool>>();
                foreach (var r in raw)
                {
                    if (!_permissionOverrides.ContainsKey(r.Command))
                        _permissionOverrides.Add(r.Command, new Dictionary<ulong, bool>() {{r.User, r.Permission}});
                    else
                        _permissionOverrides[r.Command][r.User] = r.Permission;
                    count++;
                }
                Console.WriteLine($"Found {count}.");
            }
        }

        public void SaveOverrides()
        {
            using (var writer = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PermissionOverride.xml")))
            {
                var l = new List<CommandOverride>();
                foreach (var e in _permissionOverrides)
                {
                    foreach (var p in e.Value)
                    {
                        l.Add(new CommandOverride(e.Key, p.Key, p.Value));
                    }
                }

                var x = new XmlSerializer(typeof(List<CommandOverride>));
                x.Serialize(writer, l);
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
                //var types = GetTypesSafely(assembly);
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    try
                    {
                        if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IChatCommand)))
                        {
                            Console.WriteLine("Found: " + type.FullName);
                            ChatCommands.Add((IChatCommand)Activator.CreateInstance(type));
                        }
                        if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IAutoCommand)))
                        {
                            Console.WriteLine("Found: " + type.FullName);
                            SystemAuto.Add((IAutoCommand)Activator.CreateInstance(type));
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

        private async Task RexbotClient_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            var message = e.Message;
            var channel = e.Channel;

#if DEBUG
            if (message.Author.Id != REXXAR_ID && message.Author.Id != 210339391644631040)
                return;
#endif

            if (message.Author.Id == REXBOT_ID || message.Author.Id == 186606257317085184)
                return;

            if(message.Author.Id == REXXAR_ID && message.Attachments.FirstOrDefault()?.FileName.Equals("RexBot.exe", StringComparison.CurrentCultureIgnoreCase) == true)
            {        
                const string BAT = @"@ECHO OFF
SLEEP 20
DEL RexBot.exe
REN RexBot.new RexBot.exe
start RexBot.exe";
                using (var client = new WebClient())
                {
                    string FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RexBot.new");
                    client.DownloadFile(message.Attachments.First().Url, FileName);
                }

                await message.Channel.SendMessageAsync("Received update. Restarting...");

                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.bat");

                if (!File.Exists(path))
                    File.WriteAllText(path, BAT);

                Process.Start(path);
                Environment.Exit(1);
            }

            //if (message.MentionedUsers.Any(u => u.Id == REXXAR_ID))
            //{
                //if (Regex.IsMatch(message.Content, @"fix.*it", RegexOptions.IgnoreCase))
                //{
                //    Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                //    await channel.SendMessageAsync($"{message.Author.Mention} {FIXIT_RESPONSE}");
                //}
            //}

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

                    bool? ovr = null;
                    Dictionary<ulong, bool> d;
                    if(_permissionOverrides.TryGetValue(command.Command, out d))
                    {
                        bool b;
                        if (d.TryGetValue(message.Author.Id, out b))
                            ovr = b;
                    }

                    if (ovr == false || (!command.IsPublic && (message.Author.Id != REXXAR_ID) && ovr != true))
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
                            var em = new DiscordEmbedBuilder {ImageUrl = command.Response};
                            await channel.SendMessageAsync(message.Author.Mention, embed: em.Build());
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

                    bool? ovr = null;
                    Dictionary<ulong, bool> d;
                    if (_permissionOverrides != null && _permissionOverrides.TryGetValue(command.Command, out d))
                    {
                        bool b;
                        if (d.TryGetValue(message.Author.Id, out b))
                            ovr = b;
                    }

                    Console.WriteLine($"{DateTime.Now} [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    if (ovr == false || ovr != true && !command.HasAccess(message.Author))
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

            foreach (var auto in SystemAuto)
            {
                string response;
                if (auto.Pattern.IsMatch(message.Content))
                {
                    try
                    {
                        response = await auto.Handle(message);
                    }
                    catch(Exception ex)
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

            foreach (var auto in AutoCommands)
            {
                if (Regex.IsMatch(message.Content, auto.Pattern, RegexOptions.IgnoreCase))
                    await channel.SendMessageAsync($"{message.Author.Mention} {auto.Response}");
            }
        }

        private async Task RexxarClient_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            var message = e.Message;
            var channel = message.Channel;

            //if (channel.Id == 269660270769471488)
            //{
            //    await ((SocketUserMessage)message).AddReactionAsync(":xocKappa:269884941502775306");
            //}

            if (message.Author.Id == RexxarClient.CurrentUser.Id)
                return;

            if (channel is DiscordDmChannel)
            {
                bool asking = Regex.IsMatch(message.Content, @"can.*ask.*question|have.*question", RegexOptions.IgnoreCase);
                IEnumerable<DiscordMessage> messages = await channel.GetMessagesAsync(21);
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
#if DEBUG
            return null;
#endif
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
            await RexbotClient.UpdateStatusAsync(new DiscordActivity(status));
            return status;
        }

        public async Task GetMissingHistory()
        {
            long count = 0;
            bool updating = true;
            long minDate = long.MaxValue;
            long minDateLocal = long.MaxValue;
            List<DiscordMessage> _messages = new List<DiscordMessage>();
            DiscordChannel _currentChannel = null;

            DiscordGuild server = Instance.KeenGuild;
            
            var channels = server.Channels;

            var compareTime = TimeSpan.FromDays(30);
            var member = await server.GetMemberAsyncSafe(REXBOT_ID);

            foreach (var channel in channels)
            {
                try
                {
                    if (channel == null)
                        continue;

                    if (channel.Type != ChannelType.Text)
                        continue;

                    Console.WriteLine($"Switching to {channel.Name}");
                    
                    if(!channel.PermissionsFor(member).HasFlag(Permissions.ReadMessageHistory))
                    {
                        Console.WriteLine("No permission here :(");
                        continue;
                    }
                    
                    var tmp = (await channel.GetMessagesAsync()).ToList();
                   
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

                        tmp = (await channel.GetMessagesAsync(100, tmp.First(m => m.Timestamp.UtcTicks == tmp.Min(n => n.Timestamp.UtcTicks)).Id)).ToList();
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

        [Serializable]
        public struct AutoCommand
        {
            public string Pattern;
            public string Response;
            public ulong Author;

            public AutoCommand(string pattern, string response, ulong author)
            {
                Pattern = pattern;
                Response = response;
                Author = author;
            }
        }

        [Serializable]
        public struct CommandOverride
        {
            public string Command;
            public ulong User;
            public bool Permission;

            public CommandOverride(string command, ulong user, bool permission)
            {
                Command = command;
                User = user;
                Permission = permission;
            }
        }
    }
}