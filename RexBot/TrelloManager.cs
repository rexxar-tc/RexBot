using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Manatee.Trello;
using Manatee.Trello.ManateeJson;
using Manatee.Trello.WebApi;

namespace RexBot
{
    public class TrelloManager
    {
        public readonly TrelloBoard PublicBoard;
        public readonly TrelloBoard CTGBoard;
        
        public TrelloManager(string key, string token, string publicBoard, string ctgBoard)
        {
            Console.WriteLine("Initializing Trello...");

            var serializer = new ManateeSerializer();
            TrelloConfiguration.Serializer = serializer;
            TrelloConfiguration.Deserializer = serializer;
            TrelloConfiguration.JsonFactory = new ManateeFactory();
            TrelloConfiguration.RestClientProvider = new WebApiClientProvider();
            TrelloAuthorization.Default.AppKey = key;
            TrelloAuthorization.Default.UserToken = token;

            PublicBoard = new TrelloBoard(publicBoard);
            CTGBoard = new TrelloBoard(ctgBoard);
        }

        public string AddIssue(CachedIssue issue)
        {
            TrelloBoard board = GetBoard(issue);
            List list = GetList(issue);

            var ind = issue.Issue.Description.IndexOf("\r\nReported by:");
            string desc;
            if (ind == -1)
                desc = issue.Issue.Description;
            else
                desc = issue.Issue.Description.Substring(0, ind);

            var card = list.Cards.Add($"{issue.Key}: {issue.Issue.Summary}");
            card.Description = desc;
            if (issue.Issue.Status.Name == "Resolved" || issue.Issue.Status.Name == "Cancel" || issue.Issue.Status.Name == "Implemented")
                card.IsComplete = true;

            if (issue.Comments.Any())
            {
                foreach(var comment in issue.Comments)
                    card.Comments.Add($"{comment.Author}: {comment.Body}");
            }

            var attachments = issue.Issue.GetAttachmentsAsync().Result;
            if (attachments.Any())
            {
                Parallel.ForEach(attachments, a =>
                                              {
                                                  try
                                                  {
                                                      string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tmp", card.Id + a.FileName);
                                                      a.Download(p);
                                                      var b = File.ReadAllBytes(p);
                                                      if (b.Length > 10000000)
                                                      {
                                                          Console.WriteLine("Attachment too large");
                                                          return;
                                                      }
                                                      File.Delete(p);

                                                      card.Attachments.Add(b, a.FileName);
                                                  }
                                                  catch(Exception ex)
                                                  { Console.WriteLine(ex);}
                                              });
            }

            return card.Url;
        }

        public void AddIssues(IEnumerable<CachedIssue> issues)
        {
            foreach (var i in issues)
            {
                string url = AddIssue(i);
                Console.WriteLine($"Added {i.Key} at {url}");
            }
        }

        public static void Clean(Board board)
        {
            var cds = board.Cards.Where(c => c.IsArchived == true);
            foreach(var c in cds)
                c.Delete();
        }

        public void UpdateOrAdd(CachedIssue issue)
        {
            var board = GetBoard(issue);
            var card = board.Board.Cards.FirstOrDefault(c => c.Name.StartsWith(issue.Key));
            if (card == null)
            {
                AddIssue(issue);
                return;
            }

            var newList = GetList(issue);
            if (newList != card.List)
            {
                card.List = newList;
            }

            int cardCount = card.Comments.Count();
            int jiraCount = issue.Comments.Count();

            if (cardCount < jiraCount)
            {
                foreach (var c in issue.Comments.Skip(cardCount))
                    card.Comments.Add($"{c.Author}: {c.Body}");
            }

            if (card.IsComplete != true)
            {
                if (issue.Issue.Status.Name == "Resolved" || issue.Issue.Status.Name == "Cancel" || issue.Issue.Status.Name == "Implemented")
                    card.IsComplete = true;
            }
        }

        public void UpdateOrAddMany(IEnumerable<CachedIssue> issues)
        {
            foreach(var i in issues)
                UpdateOrAdd(i);
        }

        public List GetList(CachedIssue issue)
        {
            TrelloBoard board = GetBoard(issue);

            switch (issue.Issue.Status.Name)
            {
                case "To Do":
                    return issue.Project == JiraManager.ProjectKey.SE ? board.ToDoSE : board.ToDoME;
                case "In Progress":
                    return board.InProgress;
                default:
                    return issue.Project == JiraManager.ProjectKey.SE ? board.DoneSE : board.DoneME;
            }
        }

        public TrelloBoard GetBoard(CachedIssue issue)
        {
            return issue.Metadata.IsCTG ? CTGBoard : PublicBoard;
        }
    }

    public class TrelloBoard
    {
        public readonly Board Board;
        public readonly List ToDoSE;
        public readonly List ToDoME;
        public readonly List InProgress;
        public readonly List DoneSE;
        public readonly List DoneME;

        public TrelloBoard(string id)
        {
            Board = new Board(id);
            TrelloManager.Clean(Board);
            foreach (var l in Board.Lists)
            {
                if (l.Name == "To Do - SE")
                    ToDoSE = l;
                else if (l.Name == "To Do - ME")
                    ToDoME = l;
                else if (l.Name == "In Progress")
                    InProgress = l;
                else if (l.Name == "Done - SE")
                    DoneSE = l;
                else if (l.Name == "Done - ME")
                    DoneME = l;
            }
        }
    }
}
