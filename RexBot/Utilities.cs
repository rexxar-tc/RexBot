using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using RexBot.Commands;

namespace RexBot
{
    public static class Utilities
    {
        public static string[] ParseCommand(string input)
        {
            MatchCollection matches = Regex.Matches(input, "(\"[^\"]+\"|\\S+)");
            var result = new string[matches.Count - 1];
            for (int i = 0; i < result.Length; i++)
                result[i] = matches[i + 1].Value;
            return result;
        }

        public static string StripCommand(IChatCommand command, string input)
        {
            try
            {
                return input.Substring(command.Command.Length + 1);
            }
            catch
            {
                return null;
            }
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            var bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
                dest.Write(bytes, 0, cnt);
        }

        public static string CompressToString(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            {
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    {
                        //msi.CopyTo(gs);
                        CopyTo(msi, gs);
                    }

                    return Convert.ToBase64String(mso.ToArray());
                }
            }
        }

        public static byte[] DecompressFromString(string compStr)
        {
            using (var msi = new MemoryStream(Convert.FromBase64String(compStr)))
            {
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                    {
                        //gs.CopyTo(mso);
                        CopyTo(gs, mso);
                    }

                    return mso.ToArray();
                }
            }
        }

        private static ISocketMessageChannel _logChannel = null;

        private static ISocketMessageChannel LogChannel => _logChannel ??
                                                           (_logChannel =
                                                               (ISocketMessageChannel)RexBotCore.Instance.RexbotClient.GetChannel(
                                                                   314229416509308928));

        public static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
            LogChannel.SendMessageAsync(message);
        }

        public static void Log(object obj)
        {
            Log(obj.ToString());
        }

        public static bool HasAccess(CommandAccess level, SocketUser user)
        {
            var guilduser = RexBotCore.Instance.KeenGuild.GetUser(user.Id);
            switch (level)
            {
                case CommandAccess.None:
                    return false;
                case CommandAccess.Public:
                    return true;
                case CommandAccess.Modder:
                    if (guilduser == null)
                        goto case CommandAccess.Rexxar;

                    if (guilduser.Roles.Any(r => r.Id == 279560526931951626))
                        return true;
                    goto case CommandAccess.Developer;
                case CommandAccess.Developer:
                    if (user.Id == RexBotCore.REXXAR_ID)
                        return true;
                    if (guilduser == null)
                        goto case CommandAccess.Rexxar;

                    return guilduser.Roles.Any(r => r.Id == 125014635383357440ul);
                case CommandAccess.Rexxar:
                    return user.Id == RexBotCore.REXXAR_ID;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static readonly HashSet<ulong> CTGChannels = new HashSet<ulong>() { 166886199200448512, 222685377201307648, 166899016045559808, 294875742704107520 };
    }

    public static class Extensions
    {
        private static readonly Random _random = new Random();

        public static bool HasAccess(this IChatCommand command, SocketUser user)
        {
            return Utilities.HasAccess(command.Access, user);
        }

        public static string ServerName(this ISocketMessageChannel channel)
        {
            var guildChannel = channel as SocketGuildChannel;
            return guildChannel?.Guild.Name ?? "Private";
        }

        public static List<T> Copy<T>(this List<T> input)
        {
            var output = new List<T>(input.Count);
            for (int i = 0; i < input.Count; i++)
                output[i] = input[i];
            return output;
        }

        public static T RandomElement<T>(this List<T> input)
        {
            if (input.Count == 1)
                return input[0];
            if (input.Count == 0)
                return default(T);
            int index = _random.Next(0, input.Count);
            return input[index];
        }

        public static T RandomElement<T>(this IEnumerable<T> input)
        {
            var count = input.Count();
            if (count == 1)
                return input.First();
            if (count == 0)
                return default(T);
            int index = _random.Next(0, count);
            var e = input.GetEnumerator();
            for (int i = 0; i < index; i++)
                e.MoveNext();
            return e.Current;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
        }

        public static void AddOrUpdate<T>(this Dictionary<T, int> dic, T key, int value)
        {
            if (dic.ContainsKey(key))
                dic[key] += value;
            else
                dic[key] = value;
        }

        public static bool CTG(this SocketMessage msg)
        {
            return Utilities.CTGChannels.Contains(msg.Channel.Id);
        }

        public static string NickOrUserName(this SocketUser user)
        {
            SocketGuildUser gu = user as SocketGuildUser;
           
            if (!string.IsNullOrEmpty(gu?.Nickname))
                return gu.Nickname;

            return user.Username;
        }

        public static bool CTG(this SocketUser user)
        {
            var c = RexBotCore.Instance.RexbotClient.GetChannel(166886199200448512);

            return c.GetUser(user.Id) != null;
        }

        public static bool IsRexxar(this SocketUser user)
        {
            return user.Id == RexBotCore.REXXAR_ID;
        }

        public static string NickOrUserName(this SocketGuildUser user)
        {
            if (!string.IsNullOrEmpty(user?.Nickname))
                return user.Nickname;

            return user?.Username;
        }
    }
}