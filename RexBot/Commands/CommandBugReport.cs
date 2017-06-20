using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    internal class CommandBugReport : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!bugreport";

        public string HelpText => "Sends a bug report to Keen. Available subcommands:\r\n" +
                                  "```!bugreport begin - RexBot will walk you through a new bug report step by step.\r\n" +
                                  //"!bugreport new [version] \"[summary]\" \"[description]\" - Inserts a new bugreport fully filled out.\r\n" +
                                  "!bugreport list - Lists the current active bugs.\r\n" +
                                  //"!bugreport vote [index] - Add a vote to the report. Vote if you agree with or have the same issue.\r\n" +
                                  "!bugreport add [report number] [message] - Adds additional details to the report. Only available to the original reporter." +
                                  "```\r\n" +
                                  "Please make your bug report as detailed as possible. A good bug report includes steps to reproduce the problem, your game version, and the game log located at `%AppData%\\SpaceEngineers\\SpaceEngineers.log`\r\n" +
                                  "Any files you attach to your message will be submitted with your report. Logs and screenshots are very useful.";

        public async Task<string> Handle(SocketMessage message)
        {
            ulong? channelid = (message.Channel as IGuildChannel)?.GuildId;
            if (channelid.HasValue && !((channelid == 125011928711036928) || (channelid == 263612647579189248)))
                return "This command is only available in the KSH discord.";
            
            Task jiraTask = Task.Run(() =>
                                     {
                                         string resp = Handle_Internal(message).Result;
                                         if(!string.IsNullOrEmpty(resp))
                                            message.Channel.SendMessageAsync($"{message.Author.Mention} {resp}");
                                     });

            DateTime start = DateTime.Now;
#pragma warning disable 4014 //warning lines under lambdas are incredibly annoying
            Task.Run(() =>
                     {
                         while (!jiraTask.IsCompleted)
                         {
                             if (DateTime.Now - start > TimeSpan.FromSeconds(10))
                             {
                                 message.Channel.SendMessageAsync($"{message.Author.Mention} Sorry, your report has been received, but it's taking longer than expected to process. Please **do not** submit the report again, just wait.");
                                 return;
                             }
                             Thread.Sleep(10);
                         }
                     });
#pragma warning restore 4014

            return null;
        }

        private readonly Dictionary<ulong, SocketMessage> _toConfirm = new Dictionary<ulong, SocketMessage>();

        public async Task<string> Handle_Internal(SocketMessage message, bool confirmed = false)
        {
            bool ctg = message.CTG();

            string arg = Utilities.StripCommand(this, message.Content);

            if (arg == null)
                return "Help for !bugreport: \r\n" + HelpText;

            string[] parts = Utilities.ParseCommand(message.Content);

            Console.WriteLine(string.Join(", ", parts));

            if (parts[0].Equals("delaytest"))
            {
                Thread.Sleep(30000);
                return "done";
            }

            if (parts[0].Equals("list", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!ctg)
                {
                    var thing = new EmbedBuilder();
                    thing.AddField(x =>
                                   {
                                       x.Name = "Public List";
                                       x.Value = "https://docs.google.com/spreadsheets/d/17u4sh7gpq5VeBypXW3ZkjXzmpCI9P4ct7hNYoTWP9mo/edit?usp=sharing";
                                   });

                    await message.Channel.SendMessageAsync(message.Author.Mention + "Lists update every 10 minutes.", embed: thing);
                    return string.Empty;
                }
                var em = new EmbedBuilder();
                em.AddField(x =>
                            {
                                x.Name = "Public List";
                                x.Value = "https://docs.google.com/spreadsheets/d/17u4sh7gpq5VeBypXW3ZkjXzmpCI9P4ct7hNYoTWP9mo/edit?usp=sharing";
                            });
                em.AddField(x =>
                            {
                                x.Name = "CTG List";
                                x.Value = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
                            });
                await message.Channel.SendMessageAsync(message.Author.Mention, embed: em);
                return null;
            }
            
            if (parts[0].Equals("add", StringComparison.CurrentCultureIgnoreCase))
            {
                var res = await RexBotCore.Instance.Jira.AddComment(parts[1], string.Join(" ", parts.Skip(2)), message);

                switch (res)
                {
                    case JiraManager.CommentAddResult.Error:
                        return "Error";
                    case JiraManager.CommentAddResult.Ok:
                        return "Comment added!";
                    case JiraManager.CommentAddResult.NotFound:
                        return "Couldn't find ticket with that number!";
                    case JiraManager.CommentAddResult.NotAuthorized:
                        return "Only the original reporter can add comments!";
                    default:
                        return null;
                }
            }

            if (parts[0].Equals("begin", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    var dm = await message.Author.CreateDMChannelAsync();
                    await dm.SendMessageAsync(BugreportBuilder.INTRO);
                    RexBotCore.Instance.BugBuilders[dm.Id]= new BugreportBuilder(message.Author.Id, dm);
                    Console.WriteLine($"Starting BugreportBuilder for {message.Author.NickOrUserName()}");
                    if (message.Channel.Id == dm.Id)
                        return null;
                    return "Great, let's handle this in PM :smiley:";
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex);
                    return "I can't send you a private message. Either you've blocked me or disaled PMs from this server :frowning: You'll need to use the forum instead.";
                }

            }
            
            if (arg.ToLower() == "help")
                return "Did you mean `!help !bugreport`? Help for !bugreport:\r\n" + HelpText;
            
            return "Sorry, the old style of bug reporting is deprecated. Please use `!bugreport begin` and I'll walk you through the new system.";
        }
    }
}