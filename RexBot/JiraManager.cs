﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Jira;
using Discord;
using Discord.WebSocket;
using ProtoBuf;
using Attachment = Discord.Attachment;
using Timer = System.Timers.Timer;

namespace RexBot
{
    [ProtoContract]
    public class IssueMetadata
    {
        public IssueMetadata(SocketMessage msg)
        {
            ReporterId = msg.Author.Id;
            ReporterChannel = msg.Channel.Id;
            ReporterName = msg.Author.Username;
            //Voters=new List<ulong>();
            IsCTG = msg.CTG();
        }

        public IssueMetadata()
        {
        }

        [ProtoMember(1)]
        public string ReporterName { get; set; }

        [ProtoMember(2)]
        public bool IsCTG { get; set; }

        [ProtoMember(3)]
        public ulong ReporterId { get; set; }

        [ProtoMember(4)]
        public ulong ReporterChannel { get; set; }

        //[ProtoMember(5)]
        //public List<ulong> Voters { get; set; }

        public string Serialize()
        {
            return Serialize(this);
        }

        public static IssueMetadata Deserialize(string str)
        {
            byte[] bytes = Utilities.DecompressFromString(str);
            using (var ms = new MemoryStream(bytes))
            {
                return (IssueMetadata)Serializer.Deserialize(typeof(IssueMetadata), ms);
            }
        }

        public static string Serialize(IssueMetadata meta)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, meta);
                return Utilities.CompressToString(ms.ToArray());
            }
        }
    }

    public class CachedIssue
    {
        public Issue Issue;
        public IssueMetadata Metadata;
        public string Key;
        public IEnumerable<Comment> Comments;

        public CachedIssue(Issue issue, IssueMetadata meta)
        {
            Issue = issue;
            Comments = issue.GetCommentsAsync().Result;
            Metadata = meta;
            Key = issue.Key.Value;
        }
    }

    public class JiraManager
    {
        public enum ProjectKey
        {
            SE,
            ME
        }

        private const string REXBOT_SPRINT = "114"; //magic, do not touch
        private const string REXBOT_EPIC = "SE-3591";
        private const string METADATA_TAG = "REXBOT METADATA:";
        public List<CachedIssue> CachedIssues;
        public List<ProjectVersion> CachedVersions;
        public readonly Jira jira;
        private Timer UpdateTimer;

        public JiraManager(string URL, string username, string password)
        {
            Console.WriteLine("Initializing Jira...");
            jira = Jira.CreateRestClient(URL, username, password);
            CachedIssues = new List<CachedIssue>();
            Task.Run(() =>
                     {
                         try
                         {
                             var p = jira.Projects.GetProjectAsync("SE").Result;
                             CachedVersions = p.GetVersionsAsync().Result.ToList();
                             Console.WriteLine($"Got {CachedVersions.Count} Versions");
                             CachedIssues = GetIssues();
                         }
                         catch (Exception ex)
                         {
                             Console.WriteLine(ex);
                         }
                         Console.WriteLine($"Found {CachedIssues.Count} issues");
                         UpdateTimer=new Timer(10 * 60 * 1000);
                         UpdateTimer.AutoReset = true;
                         UpdateTimer.Elapsed += (sender, args) => Update().RunSynchronously();
                         UpdateTimer.Start();
                     });
        }

        public async Task Update()
        {
            try
            {
                var p = await jira.Projects.GetProjectAsync("SE");
                CachedVersions = (await p.GetVersionsAsync()).ToList();
                var oldIssues = new List<CachedIssue>(CachedIssues);
                CachedIssues.Clear();
                CachedIssues = GetIssues();

                if (oldIssues.Count == 0)
                    oldIssues = CachedIssues;

                await RexBotCore.Instance.PublicSheet.EmptyPage("Public List");
                await RexBotCore.Instance.PublicSheet.EmptyPage("Old Reports");
                await RexBotCore.Instance.CTGSheet.EmptyPage("CTG Reports");
                await RexBotCore.Instance.CTGSheet.EmptyPage("Old Reports");

                IList<IList<object>>[] data = new IList<IList<object>>[4];

                for (int i = 0; i < 4; i++)
                    data[i] = new List<IList<object>>();

                foreach (var newIssue in CachedIssues)
                {
                    try
                    {
                        var oldIssue = oldIssues.FirstOrDefault(i => i.Key == newIssue.Key);

                        Console.WriteLine($"{DateTime.Now}: {oldIssue.Key}");
                        var comments = await oldIssue.Issue.GetCommentsAsync();
                        var sb = new StringBuilder();
                        foreach (var comment in comments)
                        {
                            if (comment.Author == "rex.bot")
                                sb.Append($"{oldIssue.Metadata.ReporterName}: ");
                            else
                                sb.Append($"{comment.Author}: ");
                            sb.AppendLine(comment.Body);
                            sb.AppendLine();
                        }

                        var ind = oldIssue.Issue.Description.IndexOf("\r\nReported by:");
                        string desc;
                        if (ind == -1)
                            desc = oldIssue.Issue.Description;
                        else
                            desc = oldIssue.Issue.Description.Substring(0, ind);

                        var row = new List<object>
                                  {
                                           oldIssue.Issue.Key.Value,
                                           oldIssue.Metadata.ReporterName,
                                           oldIssue.Issue.Created.Value.ToString(),
                                           desc,
                                           oldIssue.Issue.Status.Name,
                                           sb.ToString()
                                  };

                        if (oldIssue.Metadata.IsCTG || Utilities.CTGChannels.Contains(oldIssue.Metadata.ReporterChannel))
                        {
                            if (oldIssue.Issue.Status.Name == "Resolved" || oldIssue.Issue.Status.Name == "Cancel" || oldIssue.Issue.Status.Name == "Implemented")
                                //await RexBotCore.Instance.CTGSheet.AppendRow("Old Reports", row);
                                data[3].Add(row);
                            else
                                //await RexBotCore.Instance.CTGSheet.AppendRow("CTG Reports", row);
                                data[2].Add(row);
                        }
                        else
                        {
                            if (oldIssue.Issue.Status.Name == "Resolved" || oldIssue.Issue.Status.Name == "Cancel" || oldIssue.Issue.Status.Name == "Implemented")
                                //await RexBotCore.Instance.PublicSheet.AppendRow("Old Reports", row);
                                data[1].Add(row);
                            else
                                //await RexBotCore.Instance.PublicSheet.AppendRow("Public List", row);
                                data[0].Add(row);
                        }
                        if (!oldIssue.Metadata.IsCTG && Utilities.CTGChannels.Contains(oldIssue.Metadata.ReporterChannel))
                        {
                            oldIssue.Metadata.IsCTG = true;
                            RexBotCore.Instance.Jira.UpdateMetadata(oldIssue.Issue, oldIssue.Metadata);
                        }

                        //if (newIssue.Issue.Updated < oldIssue?.Issue.Updated)
                        await IssueUpdated(newIssue, oldIssue);
                        //Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception updating issue {newIssue.Key}");
                        Console.WriteLine(ex);
                    }
                }

                await RexBotCore.Instance.PublicSheet.AppendRows("Public List", data[0]);
                await RexBotCore.Instance.PublicSheet.AppendRows("Old Reports", data[1]);
                await RexBotCore.Instance.CTGSheet.AppendRows("CTG Reports", data[2]);
                await RexBotCore.Instance.CTGSheet.AppendRows("Old Reports", data[3]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task IssueUpdated(CachedIssue newIssue, CachedIssue oldIssue)
        {
            //Console.WriteLine($"Update {newIssue.Issue.Key}");
            var newComments = newIssue.Comments;
            var oldComments = oldIssue.Comments;
            //int sheetIndex;
            //if (newIssue.Metadata.IsCTG)
            //    sheetIndex = await RexBotCore.Instance.CTGSheet.FindRow("CTG Reports", newIssue.Key);
            //else
            //    sheetIndex = await RexBotCore.Instance.PublicSheet.FindRow("Public List", newIssue.Key);

            ISocketMessageChannel channel;
            if (!newIssue.Metadata.IsCTG)
                channel = (ISocketMessageChannel)RexBotCore.Instance.RexbotClient.GetChannel(136097351134740480);
            else
                channel = (ISocketMessageChannel)RexBotCore.Instance.RexbotClient.GetChannel(newIssue.Metadata.ReporterChannel);

            if (newComments.Count() > oldComments.Count())
            {
                var comment = newComments.Last();
                if (comment.Body.StartsWith("!notify", true, CultureInfo.CurrentCulture))
                    await channel.SendMessageAsync($"<@{newIssue.Metadata.ReporterId}> A developer has added a comment to your report `{newIssue.Issue.Key}: {newIssue.Issue.Summary}` ```{comment.Body.Substring("!notify ".Length)}```\r\n" +
                                                   $"You can respond with `!bugreport add {newIssue.Issue.Key}`");
                //if (newIssue.Metadata.IsCTG)
                //    await RexBotCore.Instance.CTGSheet.OverwriteCell($"'CTG Reports'!F:{sheetIndex}", string.Join("\r\n", newComments.Select(c => c.Body)));
                //else
                //    await RexBotCore.Instance.PublicSheet.OverwriteCell($"'Public List'!F:{sheetIndex}", string.Join("\r\n", newComments.Select(c => c.Body)));
            }
            
            if (newIssue.Issue.Status.ToString() != oldIssue.Issue.Status.ToString())
            {
                await channel.SendMessageAsync($"<@{newIssue.Metadata.ReporterId}> The status of your report `{newIssue.Issue.Key}: {newIssue.Issue.Summary}` has been updated to `{newIssue.Issue.Status}`.");
               
                //    if (newIssue.Metadata.IsCTG)
                //        await RexBotCore.Instance.CTGSheet.OverwriteCell($"CTG Reports!E:{sheetIndex}", newIssue.Issue.Status.ToString());
                //    else
                //        await RexBotCore.Instance.PublicSheet.OverwriteCell($"Public List!E:{sheetIndex}", newIssue.Issue.Status.ToString());
                //if (newIssue.Issue.Status.Name == "Resolved" || newIssue.Issue.Status.Name == "Cancel" || newIssue.Issue.Status.Name == "Implemented")
                //{
                //    if (newIssue.Metadata.IsCTG)
                //        await RexBotCore.Instance.CTGSheet.MoveRow("CTG Reports", "Old Reports", sheetIndex);
                //    else
                //        await RexBotCore.Instance.PublicSheet.MoveRow("Public List", "Old Reports", sheetIndex);
                //}
            }

            if (string.IsNullOrEmpty(oldIssue.Issue.Assignee) && !string.IsNullOrEmpty(newIssue.Issue.Assignee))
            {
                var dm = await RexBotCore.Instance.RexbotClient.GetDMChannelAsync(RexBotCore.REXXAR_ID);
                if (dm != null)
                    await dm.SendMessageAsync($"Jira issue {newIssue.Key} assigned to {newIssue.Issue.Assignee}");
                else
                {
                    var c = (ISocketMessageChannel)RexBotCore.Instance.RexbotClient.GetChannel(263612647579189248);
                    await c.SendMessageAsync("Null DM channel :(");
                    await c.SendMessageAsync($"Jira issue {newIssue.Key} assigned to {newIssue.Issue.Assignee}");
                }
            }
        }

        public async Task<string> AddIssue(ProjectKey project, string description, string version, IssueMetadata meta, IReadOnlyCollection<Attachment> attachments = null)
        {
            string summary;
            if (description.Length > 50)
                summary = description.Substring(0, 50) + "...";
            else
                summary = description;

            summary = summary.Replace('\n', ' ').Replace('\r', ' ');

            return await AddIssue(project, summary, description, version, meta, attachments);
        }

        public async Task<string> AddIssue(ProjectKey project, string summary, string description, string version, IssueMetadata meta, IReadOnlyCollection<Attachment> attachments = null)
        {
            if (project == ProjectKey.ME)
                return null; //Tim doesn't love us enough :(
            try
            {
                
                Issue issue = jira.CreateIssue(project.ToString());
                //issue["Sprint"] = REXBOT_SPRINT;
                issue.Type = "Bug";
                issue.Summary = $"Player Report: {summary}";
                issue.Description = $"{description}\r\n\r\n" +
                                    $"Reported by: {meta.ReporterName}\r\n" +
                                    $"CTG: {meta.IsCTG}\r\n\r\n" +
                                    $"{METADATA_TAG};{meta.Serialize()};";
               
                issue["Epic Link"] = REXBOT_EPIC;
                if (!string.IsNullOrEmpty(version))
                    issue.AffectsVersions.Add(version);

                await issue.SaveChangesAsync();

                if (attachments != null)
                {
                    Parallel.ForEach(attachments, a =>
                                                  {
                                                      using (var client = new WebClient())
                                                      {
                                                          client.DownloadFile(a.Url, a.Filename);
                                                      }
                                                  });
                    issue.AddAttachment(attachments.Select(a => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, a.Filename)).ToArray());
                    foreach (Attachment attachment in attachments)
                        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, attachment.Filename));
                }

                CachedIssues.Add(new CachedIssue(issue, meta));
                return issue.Key.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public enum CommentAddResult
        {
            Error,
            Ok,
            NotFound,
            NotAuthorized,
        }

        public async Task<CommentAddResult> AddComment(string issueKey, string comment, SocketMessage message)
        {
            try
            {
                Issue issue = null;
                try
                {
                    issue = await jira.Issues.GetIssueAsync(issueKey);
                }
                catch
                {
                    
                }

                if(issue == null)
                    return CommentAddResult.NotFound;

                //var m = GetMetadata(issue);
                //if(m.ReporterId != message.Author.Id)
                //    return CommentAddResult.NotAuthorized;

                await issue.AddCommentAsync($"Player comment: {message.Author.Username}: {comment}");

                var attachments = message.Attachments;

                if (attachments != null)
                {
                    Parallel.ForEach(attachments, a =>
                                                  {
                                                      using (var client = new WebClient())
                                                      {
                                                          client.DownloadFile(a.Url, a.Filename);
                                                      }
                                                  });
                    issue.AddAttachment(attachments.Select(a => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, a.Filename)).ToArray());
                    foreach (Attachment attachment in attachments)
                        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, attachment.Filename));
                }

                return CommentAddResult.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return CommentAddResult.Error;
            }
        }

        // Votes can't be set :(
        //public async Task<bool?> VoteIssue(string issueKey, ulong voterId)
        //{
        //    try
        //    {
        //        var issue = await jira.Issues.GetIssueAsync(issueKey);

        //        var meta = GetMetadata( issue );

        //        if ( meta == null )
        //        {
        //            Console.WriteLine("null meta");
        //            return null;
        //        }

        //        if(meta.Voters==null)
        //            meta.Voters = new List<ulong>();

        //        if ( meta.Voters.Contains(voterId))
        //            return false;

        //        if (issue.Votes.HasValue)
        //            issue.Votes++;
        //        else
        //            issue.Votes = 1;

        //        meta.Voters.Add(voterId);

        //        if ( !UpdateMetadata( issue, meta ) )
        //            return null;

        //        await issue.SaveChangesAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //        return null;
        //    }
        //}

        public List<CachedIssue> GetIssues()
        {
            IQueryable<Issue> issues = from i in jira.Issues.Queryable
                                       where i.Reporter == new LiteralMatch("Rex bot")
                                       orderby i.Created
                                       select i;
            var cache = new List<Issue>(issues);
            while (true)
            {
                var date = cache.Max(i => i.Created);
                var nex = from j in jira.Issues.Queryable
                          where j.Reporter == new LiteralMatch("Rex bot") && j.Created > date
                          orderby j.Created
                          select j;
                if (!nex.Any())
                    break;

                bool unique = false;
                foreach (var k in nex)
                {
                    if (!cache.Exists(l => l.Key.Equals(k.Key)))
                    {
                        unique = true;
                        cache.Add(k);
                    }
                }
                if (!unique)
                    break;
            }

            var result = new List<CachedIssue>();
            foreach (Issue issue in cache)
            {
                var c = new CachedIssue(issue, GetMetadata(issue));
                if (c.Metadata == null)
                    continue;
                result.Add(c);
            }
            
            return result;
        }

        public IssueMetadata GetMetadata(Issue issue)
        {
            try
            {
            string[] splits = issue.Description.Split(new[] {METADATA_TAG}, StringSplitOptions.None);
            if (splits.Length != 2)
            {
                Console.WriteLine($"Expected 2, got {splits.Length}: {issue.Key}");
                return null;
            }
            string[] subsplit = splits[1].Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            string data = subsplit[0].Trim(':');
                return IssueMetadata.Deserialize(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to deserialize metadata");
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public bool UpdateMetadata(Issue issue, IssueMetadata meta)
        {
            string[] splits = issue.Description.Split(new[] {METADATA_TAG}, StringSplitOptions.None);
            if (splits.Length != 2)
                return false;

            issue.Description = issue.Description.Substring(0, issue.Description.Length - splits[1].Length) + meta.Serialize();

            return true;
        }
    }
}