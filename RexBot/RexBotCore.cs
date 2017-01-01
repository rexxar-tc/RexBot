using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord;
using Discord.WebSocket;

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
            public bool RexxarOnly;
            public bool ImageResponse;

            public InfoCommand( string command, string response, bool rexxarOnly, bool imageResponse )
            {
                Command = command;
                Response = response;
                RexxarOnly = rexxarOnly;
                ImageResponse = imageResponse;
            }
        }

        private DiscordSocketClient _rexxarClient;
        private DiscordSocketClient _rexbotClient;

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

        private const long REXXAR_ID = 135116459675222016;
        private const long REXBOT_ID = 264301401801228289;

        static void Main(string[] args) => new RexBotCore().Run().GetAwaiter().GetResult();

        public async Task Run()
        {
            Console.WriteLine("initializing...");
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
                reader.Close();
            }
                await Login(tokens);
            await Task.Delay(-1);
        }

        async Task<bool> Login(List<Token> tokens )
        {
            try
            {
                if (_rexxarClient == null)
                    _rexxarClient = new DiscordSocketClient();
                await _rexxarClient.LoginAsync( TokenType.User, tokens.First( t => t.Name == "rexxar" ).Value);
                await _rexxarClient.ConnectAsync();
                _rexxarClient.MessageReceived += RexxarMessageReceived;

                if(_rexbotClient == null)
                    _rexbotClient = new DiscordSocketClient();
                await _rexbotClient.LoginAsync(TokenType.Bot, tokens.First(t => t.Name == "rexbot").Value);
                await _rexbotClient.ConnectAsync();
                await _rexbotClient.SetGame("GoodAI");
                _rexbotClient.MessageReceived += RexbotMessageReceived;

                Console.WriteLine("ready");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid Login");
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }

        private async Task RexbotMessageReceived(SocketMessage message)
        {
            var channel = message.Channel;

            if (message.Author.Id == REXBOT_ID)
                return;

            if (message.Content.Equals("meark pls", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "xocBoss.gif"));
            }

            if (message.Content.ToLower().StartsWith("!approve"))
            {
                if (message.Author.Id == _rexxarClient.CurrentUser.Id)
                {
                    var splits = message.Content.Split(' ');
                    Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    if (splits.Length > 1)
                        await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"approval{splits[1]}.png"));
                    else
                        await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "approval0.png"));
                }
                else
                {
                    await channel.SendMessageAsync($"{message.Author.Mention} I don't take orders from you!");
                }
            }

            if (message.Content.Equals("!fixit", StringComparison.CurrentCultureIgnoreCase) || message.Content.Equals("!fixxit", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i_fix.png"));
            }
            
            if (message.Content.Equals("!keen", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KEEN.jpg"));
            }

            if (message.Content.Equals("!crit", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                await channel.SendMessageAsync("Modders, complain at me! ModAPI Critical Issues thread: http://forum.keenswh.com/threads/modapi-critical-issues.7390963/");
            }
            
            if (message.Content.Equals("!bug", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                await channel.SendMessageAsync("KSH bug report forum: http://praiseclang.com");
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

            if (message.Author.Id == _rexxarClient.CurrentUser.Id)
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
            if (message.MentionedUsers.Any(u => u.Id == _rexxarClient.CurrentUser.Id) && asking)
            {
                Console.WriteLine($"{DateTime.Now}: [{channel.ServerName()}: {channel.Name}] {message.Author.Username}: {message.Content}");
                    Console.WriteLine("Responding in DM");
                    var chan = await message.Author.CreateDMChannelAsync();
                    await chan.SendMessageAsync(INTRO_MSG);
                    await chan.SendMessageAsync(ASKING_RESPONSE);
                
            }
        }
    }

    public static class Extensions
    {
        public static string ServerName( this ISocketMessageChannel channel )
        {
            var guildChannel = channel as SocketGuildChannel;
            return guildChannel?.Guild.Name ?? "Private";
        }
    }
}
