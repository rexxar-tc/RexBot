using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using RexBot.Commands;

namespace RexBot
{
    public class BugreportBuilder
    {
        public const int REPORT_MIN_LENGTH = 200;
        public const string INTRO = "Hi there, I'm going to help you build a new bug report for Keen Software House.\r\n" +
                                    "You can send `!cancel` at any time to cancel this bug report.\r\n" +
                                    "`!back` will take you back to the previous step.\r\n" +
                                    "`!clear` will clear the current step so you can start over.";

        private const string STEP_GAME = "To begin, please tell me which game you're reporting a bug for. Reply `ME` or `SE`.";
        private const string STEP_VERSION_ME = "Please enter the version of the game which shows the problem. It should look like this: `0.5.9`";
        private const string STEP_VERSION_SE = "Please enter the version of the game which shows the problem. It should look like this: `1.180.401`";
        private const string STEP_SUMMARY = "Next, enter a title for your report. It should be short, around 30-50 characters.";
        private const string STEP_CTG = "I see you're in CTG. Is this report for a CTG build? Just reply yes or no.";

        private const string STEP_DESCRIPTION = "Now describe your problem as thoroughly as you can. We __greatly__ prefer a format like this:\r\n" +
                                                "**Reproduction rate:** 100%\r\n\r\n" +
                                                "**Expected behavior:** \r\nSpotlight turns on. \r\n\r\n" +
                                                "**Observed behavior:** \r\nTurning on the spotlight causes it to explode violently. \r\n\r\n" +
                                                //"**Steps to reproduce:**\r\n1. Build a spotlight\r\n2. Try to turn it on in the terminal.\r\n\r\n" +
                                                "You can use Markdown to format your report: https://jira.atlassian.com/secure/WikiRendererHelpAction.jspa?section=all \r\n" +
                                                "Do note there is a minimum length requirement of 200 characters.\r\n" +
                                                "You can send your description as several messages if you want, send `!done` when you're done.";

        private const string STEP_STR = "Please describe steps to reproduce your problem. Note there is a 50 character minimum for this field. e.g.:\r\n" +
                                        "1. Build a spotlight\r\n" +
                                        "2. Try to turn it on in the terminal.\r\n" +
                                        "3. It explodes.\r\n" +
                                        "You can send your STR as several messages if you want, send `!done` when you're done.";

        private const string STEP_ATTACHMENTS = "If you want, you can add some files to your report. Simply drag the file into discord and it will be uploaded with your report.\r\n" +
                                                "It is *very* helpful if you include your game log, located at `%AppData%\\Space Engineers\\SpaceEngineers.log`\r\n" +
                                                "When you're done attaching files, just reply `!done`";

        private const string STEP_CONFIRM = "Great! Your report is ready to go. Before it's submitted, please review your report. " +
                                            "You can use `!back` to step back to any field you want to edit, or just send `!done` to send the report.";
        public readonly ulong ChannelId;
        public readonly ulong UserId;

        public string Summary;
        public string Version;
        public string STR;
        public bool BadVersion;
        public string Description;
        public bool CTG;
        private RestDMChannel DMChannel;
        private Dictionary<Attachment, string> Attachments = new Dictionary<Attachment, string>();
        public StepEnum CurrentStep;
        public JiraManager.ProjectKey Game;
        private bool _upload = true;

        public enum StepEnum
        {
            Invalid,
            //Intro,
            Game,
            CTG,
            Version,
            Summary,
            Description,
            STR,
            Attachments,
            Confirm,
            Finished,
        }

        public BugreportBuilder(ulong userId, RestDMChannel channel)
        {
            DMChannel = channel;
            ChannelId = channel.Id;
            UserId = userId;
            CurrentStep = StepEnum.Game;
            DMChannel.SendMessageAsync(STEP_GAME);
        }

        public async Task Process(SocketMessage msg)
        {
            if (msg == null)
                return;
            
            Utilities.Log($"BugBuilder: {msg.Author.NickOrUserName()}: {CurrentStep}: {msg.Content}");

            if (msg.Content.Equals("!cancel", StringComparison.CurrentCultureIgnoreCase))
            {
                await DMChannel.SendMessageAsync("Okay, cancelling this report.");
                CurrentStep = StepEnum.Finished;
                return;
            }

            if (msg.Content.Equals("!test", StringComparison.CurrentCultureIgnoreCase))
            {
                await DMChannel.SendMessageAsync("Okay, I won't upload this bug report.");
                _upload = false;
                return;
            }

            if (msg.Content.Equals("!back", StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentStep--;
                if (!msg.Author.CTG() && CurrentStep == StepEnum.CTG)
                    CurrentStep--;

                if(CurrentStep < StepEnum.Game)
                    CurrentStep = StepEnum.Game;
                await DMChannel.SendMessageAsync($"Backtracked to {CurrentStep}");
                return;
            }

            if (msg.Attachments != null && msg.Attachments.Count != 0 && CurrentStep != StepEnum.Attachments)
            {
                await DMChannel.SendMessageAsync("Sorry, attachments are not accepted in this step. Please upload all attachments during the attachment step.");
            }

            if (msg.Content.Equals("!clear", StringComparison.CurrentCultureIgnoreCase))
            {
                switch (CurrentStep)
                {
                    case StepEnum.Summary:
                        Summary = string.Empty;
                        break;
                    case StepEnum.Version:
                        Version = string.Empty;
                        break;
                    case StepEnum.Description:
                        Description = string.Empty;
                        break;
                    case StepEnum.Attachments:
                        Attachments.Clear();
                        break;
                    case StepEnum.STR:
                        STR = string.Empty;
                        break;
                    case StepEnum.Game:
                    case StepEnum.CTG:
                    case StepEnum.Finished:
                        await DMChannel.SendMessageAsync("Cannot clear this step.");
                        break;
                    case StepEnum.Invalid:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                await DMChannel.SendMessageAsync($"Cleared data for {CurrentStep}");
                return;
            }


            if (CurrentStep == StepEnum.Game)
            {
                if (msg.Content.Equals("SE", StringComparison.CurrentCultureIgnoreCase))
                {
                    Game = JiraManager.ProjectKey.SE;
                    CurrentStep = StepEnum.Version;
                    await DMChannel.SendMessageAsync(STEP_VERSION_SE);
                    return;
                }
                else if (msg.Content.Equals("ME", StringComparison.CurrentCultureIgnoreCase))
                {
                    Game = JiraManager.ProjectKey.ME;
                    CurrentStep = StepEnum.Version;
                    await DMChannel.SendMessageAsync(STEP_VERSION_ME);
                    return;
                }
                else
                {
                    await DMChannel.SendMessageAsync("Sorry, I didn't understand your game selection. Please respond `ME` or `SE`");
                    return;
                }
            }
            
            switch (CurrentStep)
            {
                case StepEnum.Summary:
                    if (msg.Content.Equals("!done", StringComparison.CurrentCultureIgnoreCase))
                    {
                        await DMChannel.SendMessageAsync(STEP_DESCRIPTION);
                        CurrentStep = StepEnum.Description;
                        break;
                    }

                    Summary = msg.Content;
                    await DMChannel.SendMessageAsync(STEP_DESCRIPTION);
                    CurrentStep = StepEnum.Description;
                    break;
                case StepEnum.Version:
                    if (msg.Content.Equals("!continue", StringComparison.CurrentCultureIgnoreCase))
                    {
                        BadVersion = true;
                        if (msg.Author.CTG())
                        {
                            await DMChannel.SendMessageAsync(STEP_CTG);
                            CurrentStep = StepEnum.CTG;
                        }
                        else
                        {
                            await DMChannel.SendMessageAsync(STEP_SUMMARY);
                            CurrentStep = StepEnum.Summary;
                        }
                        break;
                    }
                    if (Game == JiraManager.ProjectKey.SE)
                    {
                        if (!msg.Content.All(c => c == '.' || (c >= '0' && c <= '9')))
                        {
                            await DMChannel.SendMessageAsync("Sorry, I didn't understand your version number. It should look like `1.180.401`");
                            break;
                        }
                        var split = msg.Content.Split('.');
                        if (split.Length != 3)
                        {
                            await DMChannel.SendMessageAsync("Sorry, I didn't understand your version number. It should look like `1.180.401`");
                            break;
                        }
                        Version = split[1] + "." + split[2].Substring(0, 1);
                        if (RexBotCore.Instance.Jira.CachedVersions.All(v => v.Name != Version))
                        {
                            await DMChannel.SendMessageAsync("Sorry, either you mistyped the version number, or this version is not in my system. If you're sure the version number is right, send `!continue`");
                            break;
                        }
                    }
                    else
                    {
                        if (!msg.Content.All(c => c == '.' || (c >= '0' && c <= '9')))
                        {
                            await DMChannel.SendMessageAsync("Sorry, I didn't understand your version number. It should look like `0.5.9`");
                            break;
                        }
                        var split = msg.Content.Split('.');
                        if (split.Length != 3)
                        {
                            await DMChannel.SendMessageAsync("Sorry, I didn't understand your version number. It should look like `0.5.9`");
                            break;
                        }
                        Version = "0." + split[1] + "." + split[2];
                        if (RexBotCore.Instance.Jira.CachedVersions.All(v => v.Name != Version))
                        {
                            await DMChannel.SendMessageAsync("Sorry, either you mistyped the version number, or this version is not in my system. If you're sure the version number is right, send `!continue`");
                            break;
                        }
                    }
                    if (msg.Author.CTG())
                    {
                        await DMChannel.SendMessageAsync(STEP_CTG);
                        CurrentStep = StepEnum.CTG;
                    }
                    else
                    {
                        await DMChannel.SendMessageAsync(STEP_SUMMARY);
                        CurrentStep = StepEnum.Summary;
                    }
                    break;
                case StepEnum.CTG:
                    if (msg.Content.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
                        CTG = true;
                    else if (msg.Content.Equals("no", StringComparison.CurrentCultureIgnoreCase))
                        CTG = false;
                    else
                    {
                        await DMChannel.SendMessageAsync("Sorry, I didn't get that");
                        break;
                    }
                    await DMChannel.SendMessageAsync(STEP_SUMMARY);
                    CurrentStep = StepEnum.Summary;
                    break;
                case StepEnum.Description:
                    if (msg.Content.Equals("!done", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (!msg.Author.CTG() && Description.Length < REPORT_MIN_LENGTH)
                        {
                            await DMChannel.SendMessageAsync($"Sorry, your description did not meet the minimum requirement of {REPORT_MIN_LENGTH} characters. Please be as detailed as possible.");
                            break;
                        }
                        await DMChannel.SendMessageAsync(STEP_STR);
                        CurrentStep = StepEnum.STR;
                        break;
                    }
                    Description += "\r\n" + msg.Content;
                    await DMChannel.SendMessageAsync("Description updated. You can send another message to add more, or send `!done` to continue to the next step.");
                    break;

                    case StepEnum.STR:
                        if (msg.Content.Equals("!done", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (!msg.Author.CTG() && STR.Length < 50)
                            {
                                await DMChannel.SendMessageAsync("Sorry, your STR did not meet the minimum requirement of 50 characters. Please be as detailed as possible.");
                                break;
                            }
                            await DMChannel.SendMessageAsync(STEP_ATTACHMENTS);
                            CurrentStep = StepEnum.Attachments;
                            break;
                        }

                        STR += "\r\n" + msg.Content;
                    await DMChannel.SendMessageAsync("STR updated. You can send another message to add more, or send `!done` to continue to the next step.");
                    break;
                case StepEnum.Attachments:
                    if (msg.Content.Equals("!done", StringComparison.CurrentCultureIgnoreCase))
                    {
                        await DMChannel.SendMessageAsync(STEP_CONFIRM);
                        var sb = new StringBuilder();
                        sb.AppendLine($"Game: {Game}");
                        sb.AppendLine($"Version {Version}");
                        if (msg.Author.CTG())
                            sb.AppendLine($"CTG: {CTG}");
                        sb.AppendLine($"Summary: {Summary}");
                        sb.AppendLine($"Description: {Description}");
                        sb.AppendLine($"STR: {STR}");
                        sb.AppendLine($"Attachments: {Attachments.Count}");
                        
                        var st = sb.ToString();
                        if (st.Length <= 2000)
                            await DMChannel.SendMessageAsync(st);
                        else
                        {
                            for (int i = 0; i < st.Length; i += 2000)
                            {
                                await DMChannel.SendMessageAsync(st.Substring(i, Math.Min(2000, st.Length - i)));
                            }
                        }

                        CurrentStep = StepEnum.Confirm;
                    }
                    
                    if (msg.Attachments?.Count > 0)
                    {
                        if (msg.Attachments.Count > 1)
                        {
                            await DMChannel.SendMessageAsync("One attachment at a time, please.");
                            break;
                        }
                        Attachments.Add(msg.Attachments.First(), string.IsNullOrEmpty(msg.Content) ? null : msg.Content);
                        await DMChannel.SendMessageAsync($"Received {msg.Attachments.Count} attachments. Total {Attachments.Count} attachments to this report.");
                    }
                    break;
                    case StepEnum.Confirm:
                    if(msg.Content.Equals("!done", StringComparison.CurrentCultureIgnoreCase))
                        SendReport(msg);
                        break;
                case StepEnum.Finished:
                    break;
                case StepEnum.Invalid:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SendReport(SocketMessage msg)
        {
            var ind = Summary.IndexOf("game breaking", StringComparison.CurrentCultureIgnoreCase);
            if (ind >= 0)
                Summary = Summary.Remove(ind, 13);

            Description += "\r\n\r\nSTR:\r\n" + STR;
            Console.WriteLine($"Finished bug report for {msg.Author.NickOrUserName()}");
            Console.WriteLine($"Summary: {Summary}");
            Console.WriteLine($"Description: {Description}");
            //await DMChannel.SendMessageAsync("Great, it works");
            //return;
            var jiraTask = Task.Run(async () =>
                                    {
                                        string key;
                                        if (_upload)
                                        {
                                            if (!BadVersion)
                                                key = await RexBotCore.Instance.Jira.AddIssue(Game, Summary, Description, Version, new IssueMetadata(msg) {IsCTG = CTG}, Attachments);
                                            else
                                            {
                                                Description = $"*VERSION NOT IN JIRA AT TIME OF REPORT. REPORTED VERSION: {Version}*\r\n\r\n{Description}";
                                                key = await RexBotCore.Instance.Jira.AddIssue(Game, Summary, Description, null, new IssueMetadata(msg) {IsCTG = CTG}, Attachments);
                                            }
                                        }
                                        else
                                            key = "TEST";
                                        Utilities.Log($"Added ticket: {key}");
                                        await DMChannel.SendMessageAsync($"Thank you for your report! Report number: {key}");
                                    });

            var start = DateTime.Now;

            Task.Run(() =>
                     {
                         while (!jiraTask.IsCompleted)
                         {
                             if (DateTime.Now - start > TimeSpan.FromSeconds(10))
                             {
                                 DMChannel.SendMessageAsync("Sorry, your report has been received, but it's taking longer than expected to process. I'll notify you again when it's finished.");
                                 return;
                             }
                             Thread.Sleep(10);
                         }
                     });
        }
    }
}
