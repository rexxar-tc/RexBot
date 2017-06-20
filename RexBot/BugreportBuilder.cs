using System;
using System.Collections.Generic;
using System.Linq;
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
        public const string INTRO = "Hi there, I'm going to help you build a new bug report for Space Engineers.\r\n" +
                                     "In the future, you can skip this process by using `!bugreport new`\r\n" +
                                    "You can send `!cancel` at any time to cancel this bug report.";

        private const string STEP_VERSION = "To begin, please enter the version of the game which shows the problem. It should look like this: `1.180.401`";
        private const string STEP_SUMMARY = "Next, enter a short summary of your problem. It should be around 30-50 characters.";
        private const string STEP_CTG = "I see you're in CTG. Is this report for a CTG build? Just reply yes or no.";
        private const string STEP_DESCRIPTION = "Now describe your problem as thoroughly as you can. We __greatly__ prefer a format like this:\r\n" +
                                                "**Reproduction rate:** 100%\r\n\r\n" +
                                                "**Expected behavior:** \r\nSpotlight turns on. \r\n\r\n" +
                                                "**Observerd behavior:** \r\nTurning on the spotlight causes it to explode violently. \r\n\r\n" +
                                                "**Steps to reproduce:**\r\n1. Build a spotlight\r\n2. Try to turn it on in the terminal.\r\n\r\n" +
                                                "You can use Markdown to format your report: https://jira.atlassian.com/secure/WikiRendererHelpAction.jspa?section=all \r\n" +
                                                "Do note there is a minimum length requirement of 200 characters.\r\n" +
                                                "You can send your description as several messages if you want, send `!done` when you're done, or !attachments to add attachments.";

        private const string STEP_ATTACHMENTS = "If you want, you can add some files to your report. Simply drag the file into discord and it will be uploaded with your report.\r\n" +
                                                "It is *very* helpful if you include your game log, located at `%AppData%\\Space Engineers\\SpaceEngineers.log`\r\n" +
                                                "When you're done attaching files, just reply `!done`";

        private const string STEP_FINISH = "Great! Your report is ready to go. Remember you can skip this process next time. For example, this report would look like this:```!bugreport new {0} \"{1}\" \"{2}\"```";
        public readonly ulong ChannelId;
        public readonly ulong UserId;

        public string Summary;
        public string Version;
        public bool BadVersion;
        public string Description;
        public bool CTG;
        private RestDMChannel DMChannel;
        private List<Attachment> Attachments = new List<Attachment>();
        public StepEnum CurrentStep;
        private bool _upload = true;

        public enum StepEnum
        {
            Invalid,
            Intro,
            Summary,
            Version,
            CTG,
            Description,
            Attachments,
            Finished,
        }

        public BugreportBuilder(ulong userId, RestDMChannel channel)
        {
            DMChannel = channel;
            ChannelId = channel.Id;
            UserId = userId;
            CurrentStep = StepEnum.Intro;
            Process(null);
        }

        public async Task Process(SocketMessage msg)
        {
            if (msg != null)
            {
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
            Utilities.Log($"BugBuilder: {msg.Author.NickOrUserName()}: {CurrentStep}: {msg.Content}");
            }


            switch (CurrentStep)
            {
                case StepEnum.Intro:
                    //await DMChannel.SendMessageAsync(INTRO);
                    await DMChannel.SendMessageAsync(STEP_VERSION);
                    CurrentStep = StepEnum.Version;
                    break;
                case StepEnum.Summary:
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
                    Version = split[1]+"."+split[2].Substring(0,1);
                    if(RexBotCore.Instance.Jira.CachedVersions.All(v => v.Name != Version))
                    {
                        await DMChannel.SendMessageAsync("Sorry, either you mistyped the version number, or this version is not in my system. If you're sure the version number is right, send `!continue`");
                        break;
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
                    if(msg.Content.Equals("!done", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (!msg.Author.CTG() && Description.Length < REPORT_MIN_LENGTH)
                        {
                            await DMChannel.SendMessageAsync($"Sorry, your description did not meet the minimum requirement of {REPORT_MIN_LENGTH} characters. Please be as detailed as possible.");
                            break;
                        }
                        //await DMChannel.SendMessageAsync(string.Format(STEP_FINISH, Summary, Version, Description));
                        CurrentStep = StepEnum.Finished;
                        SendReport(msg);
                        break;
                    }
                    else if (msg.Content.Equals("!attachments", StringComparison.CurrentCultureIgnoreCase))
                    {

                        if (!msg.Author.CTG() && Description.Length < REPORT_MIN_LENGTH)
                        {
                            await DMChannel.SendMessageAsync($"Sorry, your description did not meet the minimum requirement of {REPORT_MIN_LENGTH} characters. Please be as detailed as possible.");
                            break;
                        }
                        await DMChannel.SendMessageAsync(STEP_ATTACHMENTS);
                    CurrentStep=StepEnum.Attachments;
                        break;
                    }
                    Description += "\r\n" + msg.Content;
                    await DMChannel.SendMessageAsync("Description updated. You can send another message to add more, or send `!done` or `!attachments`");
                    break;
                case StepEnum.Attachments:
                    if (msg.Attachments != null)
                    {
                        Attachments.AddRange(msg.Attachments);
                        await DMChannel.SendMessageAsync($"Received {msg.Attachments.Count} attachments. Total {Attachments.Count} attachments to this report.");
                    }
                    if (msg.Content.Equals("!done", StringComparison.CurrentCultureIgnoreCase))
                    {
                        //await DMChannel.SendMessageAsync(string.Format(STEP_FINISH, Version, Summary, Description));
                        CurrentStep = StepEnum.Finished;
                        SendReport(msg);
                    }
                    else if (!string.IsNullOrEmpty(msg.Content) && (msg.Attachments==null || !msg.Attachments.Any()))
                    {
                        await DMChannel.SendMessageAsync("Text input is not accepted in this step. Only file attachments.");
                    }
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
                                                key = await RexBotCore.Instance.Jira.AddIssue(JiraManager.ProjectKey.SE, Summary, Description, Version, new IssueMetadata(msg) {IsCTG = CTG});
                                            else
                                            {
                                                Description = $"*VERSION NOT IN JIRA AT TIME OF REPORT. REPORTED VERSION: {Version}*\r\n\r\n{Description}";
                                                key = await RexBotCore.Instance.Jira.AddIssue(JiraManager.ProjectKey.SE, Summary, Description, null, new IssueMetadata(msg) {IsCTG = CTG});
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
                                 DMChannel.SendMessageAsync($"Sorry, your report has been received, but it's taking longer than expected to process. Please **do not** submit the report again, just wait.");
                                 return;
                             }
                             Thread.Sleep(10);
                         }
                     });
        }
    }
}
