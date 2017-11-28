using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Atlassian.Jira;
using DSharpPlus.Entities;
using ProtoBuf;
using RexBot.Commands;
using Timer = System.Timers.Timer;

namespace RexBot
{
    [ProtoContract]
    public class IssueMetadata
    {
        public IssueMetadata(DiscordMessage msg)
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
        public JiraManager.ProjectKey Project;


        public CachedIssue(Issue issue, IssueMetadata meta)
            : this(issue, issue.Key.Value.StartsWith("SE") ? JiraManager.ProjectKey.SE : JiraManager.ProjectKey.ME, meta)
        {
        }
        

        public CachedIssue(Issue issue, JiraManager.ProjectKey project, IssueMetadata meta)
        {
            Issue = issue;
            Comments = issue.GetCommentsAsync().Result;
            Metadata = meta;
            Project = project;
            Key = issue.Key.Value;
        }
    }

    public class JiraManager
    {
        public enum ProjectKey
        {
            Invalid,
            SE,
            ME,
        }

        private const string REXBOT_SPRINT = "114"; //magic, do not touch
        private const string REXBOT_SE_EPIC = "SE-3591";
        private const string REXBOT_ME_EPIC = "ME-2092";
        private const string METADATA_TAG = "REXBOT METADATA:";
        public List<CachedIssue> CachedIssues;
        public List<ProjectVersion> CachedVersions;
        public readonly Jira jira;
        private Timer UpdateTimer;
        public string LastSpaceVersion;
        public string SpaceNews;
        public string LastMedievalVersion;
        public string MedievalNews;

        public JiraManager(string URL, string username, string password)
        {
            Console.WriteLine("Initializing Jira...");
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            jira = Jira.CreateRestClient(URL, username, password/*, new JiraRestClientSettings() {EnableRequestTrace = true}*/);
            CachedIssues = new List<CachedIssue>();
            Task.Run(() =>
                     {
                         try
                         {
                             GetNews();
                             var ps =jira.Projects.GetProjectsAsync().Result;
                             Console.WriteLine(string.Join(", ", ps.Select(pq => pq.Key)));
                             var p = jira.Projects.GetProjectAsync("SE").Result;
                             CachedVersions = p.GetVersionsAsync().Result.ToList();
                             p = jira.Projects.GetProjectAsync("ME").Result;
                             CachedVersions.AddRange(p.GetVersionsAsync().Result);
                             Console.WriteLine($"Got {CachedVersions.Count} Versions");
                             //Console.WriteLine(string.Join(", ", CachedVersions.Select(v => v.Name)));
                             GetIssues(CachedIssues);
                             RexBotCore.Instance.Trello.UpdateOrAddMany(CachedIssues);
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
                GetNews();
                var p = await jira.Projects.GetProjectAsync("SE");
                CachedVersions = (await p.GetVersionsAsync()).ToList();
                p = jira.Projects.GetProjectAsync("ME").Result;
                CachedVersions.AddRange(p.GetVersionsAsync().Result);
                var oldIssues = new List<CachedIssue>(CachedIssues);
                CachedIssues.Clear();
                GetIssues(CachedIssues);

                if (oldIssues.Count == 0)
                    oldIssues = CachedIssues;

                IList<IList<object>>[] data = new IList<IList<object>>[6];

                for (int i = 0; i < data.Length; i++)
                    data[i] = new List<IList<object>>();

                RexBotCore.Instance.Trello.UpdateOrAddMany(CachedIssues);

                foreach (var newIssue in CachedIssues)
                {
                    try
                    {
                        var oldIssue = oldIssues.FirstOrDefault(i => i.Key == newIssue.Key);

                        //Console.WriteLine($"{DateTime.Now}: {oldIssue.Key}");
                        var comments = await oldIssue.Issue.GetCommentsAsync();
                        var sb = new StringBuilder();
                        foreach (var comment in comments)
                        {
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

                        if (oldIssue.Metadata.IsCTG || Utilities.CTGChannelIds.Contains(oldIssue.Metadata.ReporterChannel))
                        {
                            if (oldIssue.Issue.Status.Name == "Resolved" || oldIssue.Issue.Status.Name == "Cancel" || oldIssue.Issue.Status.Name == "Implemented")
                                //await RexBotCore.Instance.CTGSheet.AppendRow("Old Reports", row);
                                data[5].Add(row);
                            else
                            {
                                //await RexBotCore.Instance.CTGSheet.AppendRow("CTG Reports", row);
                                if (oldIssue.Key.StartsWith("SE"))
                                    data[3].Add(row);
                                else if (oldIssue.Key.StartsWith("ME"))
                                    data[4].Add(row);
                                else
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                        {
                            if (oldIssue.Issue.Status.Name == "Resolved" || oldIssue.Issue.Status.Name == "Cancel" || oldIssue.Issue.Status.Name == "Implemented")
                                //await RexBotCore.Instance.PublicSheet.AppendRow("Old Reports", row);
                                data[2].Add(row);
                            else
                            {
                                //await RexBotCore.Instance.PublicSheet.AppendRow("Public List", row);
                                if (oldIssue.Key.StartsWith("SE"))
                                    data[0].Add(row);
                                else if (oldIssue.Key.StartsWith("ME"))
                                    data[1].Add(row);
                                else
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        if (!oldIssue.Metadata.IsCTG && Utilities.CTGChannelIds.Contains(oldIssue.Metadata.ReporterChannel))
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

                await RexBotCore.Instance.PublicSheet.EmptyPage("Public List");
                if (data[0].Any())
                    await RexBotCore.Instance.PublicSheet.AppendRows("Public List", data[0]);
                await RexBotCore.Instance.PublicSheet.EmptyPage("ME Reports");
                if (data[1].Any())
                    await RexBotCore.Instance.PublicSheet.AppendRows("ME Reports", data[1]);
                await RexBotCore.Instance.PublicSheet.EmptyPage("Old Reports");
                if (data[2].Any())
                    await RexBotCore.Instance.PublicSheet.AppendRows("Old Reports", data[2]);
                await RexBotCore.Instance.CTGSheet.EmptyPage("CTG Reports");
                if (data[3].Any())
                    await RexBotCore.Instance.CTGSheet.AppendRows("CTG Reports", data[3]);
                await RexBotCore.Instance.CTGSheet.EmptyPage("ME Reports");
                if (data[4].Any())
                    await RexBotCore.Instance.CTGSheet.AppendRows("ME Reports", data[4]);
                await RexBotCore.Instance.CTGSheet.EmptyPage("Old Reports");
                if (data[5].Any())
                    await RexBotCore.Instance.CTGSheet.AppendRows("Old Reports", data[5]);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }

        public void GetNews()
        {
            using (var client = new WebClient())
            {
                var news = client.DownloadString("http://mirror.keenswh.com/news/SpaceEngineersChangelog.xml");
                var document = XDocument.Parse(news);
                var version = (string)((IEnumerable<object>)document.XPathEvaluate("/News/Entry/@version"))
                    .Cast<XAttribute>()
                    .FirstOrDefault();
                SpaceNews = (string)((IEnumerable<object>)document.XPathEvaluate("/News/Entry"))
                    .Cast<XElement>()
                    .FirstOrDefault();
                int i;
                if (!int.TryParse(version, out i))
                    Console.WriteLine($"Failed to parse Space version! Got {version}");
                else
                    LastSpaceVersion = version[0] + "." + version.Substring(1, 3) + "." + version.Substring(4);

                news = client.DownloadString("http://mirror.keenswh.com/news/MedievalEngineersChangelog.xml");
                document = XDocument.Parse(news);
                version = (string)((IEnumerable<object>)document.XPathEvaluate("/News/Entry/@version"))
                    .Cast<XAttribute>()
                    .FirstOrDefault();
                MedievalNews = (string)((IEnumerable<object>)document.XPathEvaluate("/News/Entry"))
                    .Cast<XElement>()
                    .FirstOrDefault();

                if (!int.TryParse(version, out i))
                    Console.WriteLine($"Failed to parse Medieval version! Got {version}");
                else
                    LastMedievalVersion = "0." + version.Substring(0, 2).Trim('0') + "." + version.Substring(3).Trim('0');

                //Console.WriteLine($"Finished updating versions. SE: {LastSpaceVersion} ME: {LastMedievalVersion}");
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

            DiscordChannel channel;
            if (!newIssue.Metadata.IsCTG)
                channel = await RexBotCore.Instance.RexbotClient.GetChannelAsync(136097351134740480);
            else
            {
                if (newIssue.Issue.Project == "SE")
                    channel = await RexBotCore.Instance.RexbotClient.GetChannelAsync(166886199200448512);
                else
                    channel = await RexBotCore.Instance.RexbotClient.GetChannelAsync(222685377201307648);
            }

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
                string message = $"<@{newIssue.Metadata.ReporterId}> The status of your report `{newIssue.Issue.Key}: {newIssue.Issue.Summary}` has been updated to `{newIssue.Issue.Status}";
                if (newIssue.Issue.Status.Name.Equals("Resolved"))
                    message += $" : {newIssue.Issue.Resolution}`";
                else
                    message += '`';
                await channel.SendMessageAsync(message);

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
                Utilities.Log($"Jira issue {newIssue.Key} assigned to {newIssue.Issue.Assignee}");
            }
        }

        public async Task<CachedIssue> AddIssue(ProjectKey project, string description, string version, IssueMetadata meta, Dictionary<DiscordAttachment, string> attachments = null)
        {
            string summary;
            if (description.Length > 50)
                summary = description.Substring(0, 50) + "...";
            else
                summary = description;

            summary = summary.Replace('\n', ' ').Replace('\r', ' ');

            return await AddIssue(project, summary, description, version, meta, attachments);
        }

        public async Task<CachedIssue> AddIssue(ProjectKey project, string summary, string description, string version, IssueMetadata meta, Dictionary<DiscordAttachment, string> attachments = null)
        {
            try
            {
                Issue issue = jira.CreateIssue(project.ToString());
                //issue["Sprint"] = REXBOT_SPRINT;
                issue.Type = "Bug";
                issue.Summary = $"Player Report: {summary}";
                issue.Description = $"{description}\r\n\r\n" +
                                    $"Reported by: {meta.ReporterName} : <@{meta.ReporterId}>\r\n" +
                                    $"CTG: {meta.IsCTG}\r\n\r\n" +
                                    $"{METADATA_TAG};{meta.Serialize()};";

                switch (project)
                {
                    case ProjectKey.SE:
                        issue["Epic Link"] = REXBOT_SE_EPIC;
                        issue["Regression"] = "None";
                        break;
                    case ProjectKey.ME:
                        issue["Epic Link"] = REXBOT_ME_EPIC;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(project), project, null);
                }

                if (!string.IsNullOrEmpty(version))
                    issue.AffectsVersions.Add(version);

                issue.Labels.Add("Rexbot");

                await issue.SaveChangesAsync();

                if (attachments != null)
                {
                    //Parallel.ForEach(attachments, a =>
                    //                              {
                    //                                  using (var client = new WebClient())
                    //                                  {
                    //                                      client.DownloadFile(a.Key.Url, a.Key.FileName);
                    //                                  }
                    //                              });
                    //issue.AddAttachment();
                    //issue.AddAttachment(attachments.Select(a => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, a.FileName)).ToArray());
                    //foreach (Attachment attachment in attachments)
                    //    File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, attachment.FileName));
                    using (var client = new WebClient())
                    {
                        foreach (var e in attachments)
                        {
                            
                            var b = client.DownloadData(e.Key.Url);
                            string name;
                            if (string.IsNullOrEmpty(e.Value))
                                name = e.Key.FileName;
                            else
                            {
                                name = e.Value;
                                int ind = e.Key.FileName.LastIndexOf('.');
                                if (ind == -1)
                                    name = e.Key.FileName;
                                else
                                    name += e.Key.FileName.Substring(ind);
                            }
                            issue.AddAttachment(name, b);
                        }
                    }
                }

                //CachedIssues.Add(new CachedIssue(issue, meta));
                //return issue.Key.Value;
                return new CachedIssue(issue, project, meta);
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex);
                Utilities.Log($"<@{RexBotCore.REXXAR_ID}> Error submitting ticket.");
                Utilities.Log(ex);
                return null;
            }
        }

        public enum JiraActionResult
        {
            Error,
            Ok,
            NotFound,
            NotAuthorized,
        }

        public async Task<JiraActionResult> AddComment(string issueKey, string comment, DiscordMessage message)
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
                    return JiraActionResult.NotFound;

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
                                                          client.DownloadFile(a.Url, a.FileName);
                                                      }
                                                  });
                    issue.AddAttachment(attachments.Select(a => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, a.FileName)).ToArray());
                    foreach (DiscordAttachment attachment in attachments)
                        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, attachment.FileName));
                }

                return JiraActionResult.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return JiraActionResult.Error;
            }
        }

        public async Task<JiraActionResult> CancelTicket(string issueKey, DiscordMessage message)
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

                if (issue == null)
                    return JiraActionResult.NotFound;

                if (!Utilities.HasAccess(CommandAccess.Developer, message.Author))
                {
                    var m = GetMetadata(issue);
                    if (m.ReporterId != message.Author.Id)
                        return JiraActionResult.NotAuthorized;
                }

                if(!string.IsNullOrEmpty(issue.Assignee) || issue.Status.Name != "To Do")
                    return JiraActionResult.NotAuthorized;

                issue.Resolution = "Won't Do";

                await issue.WorkflowTransitionAsync("Set as Resolved");
                await issue.SaveChangesAsync();

                return JiraActionResult.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return JiraActionResult.Error;
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

        public void GetIssues(List<CachedIssue> list)
        {
            IQueryable<Issue> issues = from i in jira.Issues.Queryable
                                       where i.Reporter == new LiteralMatch("Rex bot")
                                       where i.Project == "SE"
                                       orderby i.Key descending
                                       select i;
            var cache = new List<Issue>(issues);
            while (true)
            {
                var date = cache.Min(i => i.Key.Value);
                var nex = from j in jira.Issues.Queryable
                          where j.Reporter == new LiteralMatch("Rex bot") && j.Key < date
                          where j.Project == "SE"
                          orderby j.Key descending
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

            issues = from i in jira.Issues.Queryable
                     where i.Reporter == new LiteralMatch("Rex bot")
                     where i.Project == "ME"
                     orderby i.Key descending
                     select i;
            var mcache = new List<Issue>(issues);
            while (true)
            {
                var date = mcache.Min(i => i.Key.Value);
                var nex = from j in jira.Issues.Queryable
                          where j.Reporter == new LiteralMatch("Rex bot") && j.Key < date
                          where j.Project == "ME"
                          orderby j.Key descending
                          select j;
                if (!nex.Any())
                    break;

                bool unique = false;
                foreach (var k in nex)
                {
                    if (!mcache.Exists(l => l.Key.Equals(k.Key)))
                    {
                        unique = true;
                        mcache.Add(k);
                    }
                }
                if (!unique)
                    break;
            }

            cache.AddRange(mcache);
            list.Clear();
            foreach (Issue issue in cache)
            {
                var c = new CachedIssue(issue, GetMetadata(issue));
                if (c.Metadata == null)
                    continue;
                list.Add(c);
            }
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