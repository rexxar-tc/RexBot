//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Atlassian.Jira;

//namespace RexBot.ItemClasses
//{
//public    class JiraIssue
//    {
//        public class Comment
//        {
            
//        }

//        [Jsoni]
//        private Issue _issue;
//        public Issue Issue
//        {
//            get
//            {
//                return _issue ?? (_issue = RexBotCore.Instance.Jira.jira.Issues.GetIssueAsync(Key).Result); 
//               BitConverter.dou
//            }
//            private set { _issue = value; }
//        }

//        public readonly string Key;
//        public List<Comment> Comments { get; private set; }
//        public string Summary { get; private set; }
//        public string Description { get; private set; }
//        public string Status { get; private set; }

//    }
//}
