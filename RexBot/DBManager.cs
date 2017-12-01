using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;

namespace RexBot
{
    public class DBManager
    {
        private SQLiteConnection _dbConnection;

        public DBManager(string FileName)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
            if (File.Exists(path))
            {
                _dbConnection = new SQLiteConnection($"Data Source={path};Version=3;");
                _dbConnection.Open();
                RexBotCore.Instance.RexbotClient.MessageDeleted += RexbotClient_MessageDeleted;
                RexBotCore.Instance.RexbotClient.MessageCreated += RexbotClient_MessageReceived;
                RexBotCore.Instance.RexbotClient.MessageUpdated += RexbotClient_MessageUpdated;
                return;
            }
            Console.WriteLine($"DB not found, creating new at {path}");
            SQLiteConnection.CreateFile(path);

            _dbConnection = new SQLiteConnection($"Data Source={path};Version=3;");
            _dbConnection.Open();

            var keenGuild = RexBotCore.Instance.KeenGuild;

            foreach (var channel in keenGuild.Channels)
            {
                if (channel.Type != ChannelType.Text)
                    continue;

                Console.WriteLine($"Creating table for {channel.Name}: {channel.Id}");
                ExecuteNonQuery($"create table K{channel.Id} (authorId INTEGER, messageId INTEGER, timestamp INTEGER, message TEXT, edit TEXT, deleted INT, attachment TEXT, UNIQUE (messageId));");
            }

            Console.WriteLine("DB init finished.");

            RexBotCore.Instance.RexbotClient.MessageDeleted += RexbotClient_MessageDeleted;
            RexBotCore.Instance.RexbotClient.MessageCreated += RexbotClient_MessageReceived;
            RexBotCore.Instance.RexbotClient.MessageUpdated += RexbotClient_MessageUpdated;
        }

        private async Task RexbotClient_MessageUpdated(DSharpPlus.EventArgs.MessageUpdateEventArgs e)
        {
            try
            {
                if (e.Guild?.Id != RexBotCore.Instance.KeenGuild.Id)
                {
                    //Console.WriteLine("bad guild edit");
                    return;
                }
                DiscordMessage msg = e.Message;
                //Console.WriteLine(msg.Id);
                if(msg.Content == null)
                    msg = await e.Channel.GetMessageAsync(e.Message.Id);
                if(msg?.Content == null)
                    return;
                int num = ExecuteNonQuery($"UPDATE K{e.Channel.Id} SET edit = edit || '\r\n' || '{msg.Content.Replace("'", "''")}' WHERE messageId = {msg.Id}");
                // Console.WriteLine($"Recorded edit for {num} message");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private async Task RexbotClient_MessageDeleted(DSharpPlus.EventArgs.MessageDeleteEventArgs e)
        {
            if (e.Guild.Id != RexBotCore.Instance.KeenGuild.Id)
                return;

            int num = ExecuteNonQuery($"UPDATE K{e.Channel.Id} SET deleted = 1 WHERE messageId = {e.Message.Id}");
            //Console.WriteLine($"Set {num} message deleted");
        }

        public void Close()
        {
            _dbConnection.Close();
        }

        private async Task RexbotClient_MessageReceived(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            AddMessage(e.Message);
        }
        
        public void AddMessage(DiscordMessage msg)
        {
            if (msg.Channel.Guild.Id != RexBotCore.Instance.KeenGuild.Id)
                return;

            var result = ExecuteQuery($"SELECT count(*) FROM sqlite_master WHERE type='table' AND name ='K{msg.Channel.Id}'");
            result.Read();
            int res = result.GetInt32(0);
            if (res == 0)
            {
                Console.WriteLine("New channel. Making table...");
                ExecuteNonQuery($"create table K{msg.Channel.Id} (authorId INTEGER, messageId INTEGER, timestamp INTEGER, message TEXT, edit TEXT, deleted INT, attachment TEXT, unique (messageId));");
            }

            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@messagetext", msg.Content)
            };
            if (msg.Attachments.Any())
                ExecuteNonQuery($"insert into K{msg.Channel.Id} (authorId, messageId, timestamp, message, deleted, edit, attachment) values ({msg.Author.Id}, {msg.Id}, {msg.Timestamp.UtcTicks}, @messagetext, 0, ' ', '{string.Join(", ", msg.Attachments.Select(a=>a.Url))}')", parameters);
            else
                ExecuteNonQuery($"insert into K{msg.Channel.Id} (authorId, messageId, timestamp, message, deleted, edit) values ({msg.Author.Id}, {msg.Id}, {msg.Timestamp.UtcTicks}, @messagetext, 0, ' ')", parameters);
        }

        //public void AddMessage(DiscordMessage msg)
        //{
        //    if ((msg.Channel as SocketGuildChannel)?.Guild.Id != RexBotCore.Instance.KeenGuild.Id)
        //        return;

        //    SQLiteParameter[] parameters =
        //    {
        //        new SQLiteParameter("@messagetext", msg.Content)
        //    };
        //    if (msg.Attachments.Any())
        //        ExecuteNonQuery($"insert or ignore into K{msg.Channel.Id} (authorId, messageId, timestamp, message, deleted, edit, attachment) values ({msg.Author.Id}, {msg.Id}, {msg.Timestamp.UtcTicks}, @messagetext, 0, ' ', '{string.Join(", ", msg.Attachments.Select(a => a.Url))}')", parameters);
        //    else
        //        ExecuteNonQuery($"insert or ignore into K{msg.Channel.Id} (authorId, messageId, timestamp, message, deleted, edit) values ({msg.Author.Id}, {msg.Id}, {msg.Timestamp.UtcTicks}, @messagetext, 0, ' ')", parameters);
        //}

        public void AddMessages(IEnumerable<DiscordMessage> messages)
        {
            using (var transaction = _dbConnection.BeginTransaction())
            {
                foreach(var msg in messages)
                    AddMessage(msg);
                transaction.Commit();
            }
        }

        public ulong GetOldestMessageID(ulong channel)
        {
            try
            {
                var reader = ExecuteQuery($"SELECT * FROM K{channel} WHERE timestamp IS NOT NULL ORDER BY timestamp ASC");
                reader.Read();
                var val = (long)reader.GetValue(1);
                Console.WriteLine(val.ToString());
                return ulong.Parse(((long)reader.GetValue(1)).ToString());
            }
            catch
            {
                return 0;
            }
        }

        public List<MessageItem> GetMessages(ulong channel)
        {
            var output = new List<MessageItem>();

            var reader = ExecuteQuery($"SELECT * FROM K{channel}");

            while (reader.Read())
            {
                output.Add(new MessageItem(reader));
            }

            return output;
        }

        #region Internal Methods

        public int ExecuteNonQuery(string command)
        {
            try
            {
                var sql = new SQLiteCommand(command, _dbConnection);
                var num = sql.ExecuteNonQuery();
                //Console.WriteLine($"{num} rows affected by command {command}");
                return num;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Bad command?");
                Console.WriteLine(command);
                Console.WriteLine(ex);
                throw;
            }
        }
        public int ExecuteNonQuery(string command, SQLiteParameter[] parameters)
        {
            try
            {
                var sql = new SQLiteCommand(command, _dbConnection);
                sql.Parameters.AddRange(parameters);
                var num = sql.ExecuteNonQuery();
                //Console.WriteLine($"{num} rows affected by command {command}");
                return num;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Bad command?");
                Console.WriteLine(command);
                Console.WriteLine(ex);
                throw;
            }
        }

        public SQLiteDataReader ExecuteQuery(string command)
        {
            var tableCmd = new SQLiteCommand(command, _dbConnection);
            return tableCmd.ExecuteReader();
        }
        public SQLiteDataReader ExecuteQuery(string command, SQLiteParameter[] parameters)
        {
            var tableCmd = new SQLiteCommand(command, _dbConnection);
            tableCmd.Parameters.AddRange(parameters);
            return tableCmd.ExecuteReader();
        }


        #endregion
    }

    public class MessageItem
    {
        public string Content;
        public ulong Id;
        public ulong AuthorId;
        public DateTime Timestamp;
        public bool Deleted;
        public string EditHistory;

        //(authorId INTEGER, messageId INTEGER, timestamp INTEGER, message TEXT, edit TEXT, deleted INT, attachment TEXT)
        public MessageItem(SQLiteDataReader reader)
        {
            AuthorId = (ulong) reader.GetInt64(0);
            Id = (ulong) reader.GetInt64(1);

            Timestamp = new DateTime(reader.GetInt64(2));
            Content = reader.GetString(3);
            EditHistory = reader.GetString(4);
            Deleted = reader.GetInt16(5) == 1;
        }
    }
}
