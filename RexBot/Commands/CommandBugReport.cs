using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandBugReport : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!bugreport";

        public string HelpText => "Sends a bug report to Keen. Available subcommands:\r\n" +
                                  "```!bugreport begin - RexBot will walk you through a new bug report step by step.\r\n" +
                                  //"!bugreport new [version] \"[summary]\" \"[description]\" - Inserts a new bugreport fully filled out.\r\n" +
                                  "!bugreport list - Lists the current active bugs.\r\n" +
                                  //"!bugreport vote [index] - Add a vote to the report. Vote if you agree with or have the same issue.\r\n" +
                                  "!bugreport add [report number] [message] - Adds additional details to the report.\r\n" +
                                  "!bugreport cancel [report number] [message] - Cancels your ticket, and leaves a comment explaining why. Only available to the original reporter." +
                                  "```";

        public Embed HelpEmbed => _help.Value;
        private Lazy<Embed> _help = new Lazy<Embed>(CreateLazy);

        public static string PublicList;
        public static string CTGList;

        private static Embed CreateLazy()
        {
            var em = new EmbedBuilder();
            em.Author = new EmbedAuthorBuilder()
                        {
                            IconUrl = RexBotCore.Instance.RexbotClient.CurrentUser.GetAvatarUrl(),
                            Name = "!bugreport"
                        };
            em.Description = "Sends a bug report to Keen. Available subcommands:";
            em.Fields = new List<EmbedFieldBuilder>()
                        {
                            new EmbedFieldBuilder()
                            {
                                IsInline = false,
                                Name =  "!bugreport begin",
                                Value = "RexBot will walk you through a new bug report step by step."
                            },
                            new EmbedFieldBuilder()
                            {
                                IsInline = false,
                                Name = "!bugreport list",
                                Value = "Lists current active bugs."
                            },
                            new EmbedFieldBuilder()
                            {
                                IsInline = false,
                                Name="!bugreport add [report number] [message]",
                                Value =  "Adds additional details to the report."
                            },
                            new EmbedFieldBuilder()
                            {
                                IsInline = false,
                                Name = "!bugreport cancel [report number] [message]",
                                Value = "Cancels your ticket and leaves a comment explaining why. Only available to the original reporter."
                            }
                        };
            em.Color = new Color(102,153,255);
            return em.Build();
        }
        
        public async Task<string> Handle(SocketMessage message)
        {
            ulong? channelid = (message.Channel as IGuildChannel)?.GuildId;
            if (channelid.HasValue && !((channelid == 125011928711036928) || (channelid == 263612647579189248)))
                return "This command is only available in the KSH discord.";
            
            string arg = Utilities.StripCommand(this, message.Content);

            if (arg == null)
            {
                await message.Channel.SendMessageAsync(message.Author.Mention + " Help for !bugreport:", embed: _help.Value);
                return null;
            }

            string[] parts = Utilities.ParseCommand(message.Content);
            
            //return "Sorry, bugreport is temporarily disabled due to technical issues. Here's a random emoji insead: " + RexBotCore.Instance.GetRandomEmoji();

            if (parts[0].Equals("list", StringComparison.CurrentCultureIgnoreCase))
            {
                if (message.CTG() || (!(message.Channel is IGuildChannel) && message.Author.CTG()))
                {
                    var em = new EmbedBuilder();
                    em.AddField(x =>
                                {
                                    x.Name = "Public List";
                                    x.Value = $"https://docs.google.com/spreadsheets/d/{PublicList}/edit?usp=sharing";
                                });
                    em.AddField(x =>
                                {
                                    x.Name = "CTG List";
                                    x.Value = $"https://docs.google.com/spreadsheets/d/{CTGList}/edit?usp=sharing";
                                });
                    await message.Channel.SendMessageAsync(message.Author.Mention + " Lists update every 10 minutes.", embed: em.Build());
                }
                else
                {
                    var thing = new EmbedBuilder();
                    thing.AddField(x =>
                                   {
                                       x.Name = "Public List";
                                       x.Value = $"https://docs.google.com/spreadsheets/d/{PublicList}/edit?usp=sharing";
                                   });

                    await message.Channel.SendMessageAsync(message.Author.Mention + " Lists update every 10 minutes.", embed: thing.Build());
                    return string.Empty;
                }
                return null;
            }
            
            if (parts[0].Equals("add", StringComparison.CurrentCultureIgnoreCase))
            {
                var res = await RexBotCore.Instance.Jira.AddComment(parts[1], string.Join(" ", parts.Skip(2)), message);

                switch (res)
                {
                    case JiraManager.JiraActionResult.Error:
                        return "Error";
                    case JiraManager.JiraActionResult.Ok:
                        return "Comment added!";
                    case JiraManager.JiraActionResult.NotFound:
                        return "Couldn't find ticket with that number!";
                    case JiraManager.JiraActionResult.NotAuthorized:
                        return "Only the original reporter can add comments!";
                    default:
                        return null;
                }
            }

            if (parts[0].Equals("cancel", StringComparison.CurrentCultureIgnoreCase))
            {
                string key = parts[1];
                string comment = string.Join(" ", parts.Skip(2));

                var res = await RexBotCore.Instance.Jira.CancelTicket(key, message);

                switch (res)
                {
                    case JiraManager.JiraActionResult.Error:
                        return "Error";
                    case JiraManager.JiraActionResult.Ok:
                        await RexBotCore.Instance.Jira.AddComment(key, comment, message);
                        return "Ticket cancelled.";
                    case JiraManager.JiraActionResult.NotFound:
                        return "Couldn't find ticket with that number!";
                    case JiraManager.JiraActionResult.NotAuthorized:
                        return "Can't cancel this ticket. Either you are not authorized, or the ticket has already been processed by the developers.";
                    default:
                        return null;
                }
            }

            if (parts[0].Equals("begin", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    var dm = await message.Author.GetOrCreateDMChannelAsync();
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
                    return "I can't send you a private message. Either you've blocked me, or disaled PMs from this server :frowning: You'll need to use the forum instead.";
                }

            }

            if (arg.ToLower() == "help")
            {
                await message.Channel.SendMessageAsync($"{message.Author.Mention} Did you mean `!help !bugreport?", embed: _help.Value);
                return null;
            }
            
            return "Sorry, I didn't understand your request. Try `!help !bugreport`";
        }
    }
}