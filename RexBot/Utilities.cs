using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using RexBot.Commands;

namespace RexBot
{
    public static class Utilities
    {
        private static Random _random = new Random();
        //lifted from https://github.com/MauriceButler/badwords/blob/master/regexp.js
        private const string PROFANITY_REG = @"(\b4r5e\b|\b5h1t\b|\b5hit\b|\ba55\b|\banal\b|\banus\b|\bar5e\b|\barrse\b|\barse\b|\bass\b|\bass-fucker\b|\basses\b|\bassfucker\b|\bassfukka\b|\basshole\b|\bassholes\b|\basswhole\b|\ba_s_s\b|\bb!tch\b|\bb00bs\b|\bb17ch\b|\bb1tch\b|\bballbag\b|\bballs\b|\bballsack\b|\bbastard\b|\bbeastial\b|\bbeastiality\b|\bbellend\b|\bbestial\b|\bbestiality\b|\bbi+ch\b|\bbiatch\b|\bbitch\b|\bbitcher\b|\bbitchers\b|\bbitches\b|\bbitchin\b|\bbitching\b|\bbloody\b|\bblow job\b|\bblowjob\b|\bblowjobs\b|\bboiolas\b|\bbollock\b|\bbollok\b|\bboner\b|\bboob\b|\bboobs\b|\bbooobs\b|\bboooobs\b|\bbooooobs\b|\bbooooooobs\b|\bbreasts\b|\bbuceta\b|\bbugger\b|\bbum\b|\bbunny fucker\b|\bbutt\b|\bbutthole\b|\bbuttmuch\b|\bbuttplug\b|\bc0ck\b|\bc0cksucker\b|\bcarpet muncher\b|\bcawk\b|\bchink\b|\bcipa\b|\bcl1t\b|\bclit\b|\bclitoris\b|\bclits\b|\bcnut\b|\bcock\b|\bcock-sucker\b|\bcockface\b|\bcockhead\b|\bcockmunch\b|\bcockmuncher\b|\bcocks\b|\bcocksuck\b|\bcocksucked\b|\bcocksucker\b|\bcocksucking\b|\bcocksucks\b|\bcocksuka\b|\bcocksukka\b|\bcok\b|\bcokmuncher\b|\bcoksucka\b|\bcoon\b|\bcox\b|\bcrap\b|\bcum\b|\bcummer\b|\bcumming\b|\bcums\b|\bcumshot\b|\bcunilingus\b|\bcunillingus\b|\bcunnilingus\b|\bcunt\b|\bcuntlick\b|\bcuntlicker\b|\bcuntlicking\b|\bcunts\b|\bcyalis\b|\bcyberfuc\b|\bcyberfuck\b|\bcyberfucked\b|\bcyberfucker\b|\bcyberfuckers\b|\bcyberfucking\b|\bd1ck\b|\bdamn\b|\bdick\b|\bdickhead\b|\bdildo\b|\bdildos\b|\bdink\b|\bdinks\b|\bdirsa\b|\bdlck\b|\bdog-fucker\b|\bdoggin\b|\bdogging\b|\bdonkeyribber\b|\bdoosh\b|\bduche\b|\bdyke\b|\bejaculate\b|\bejaculated\b|\bejaculates\b|\bejaculating\b|\bejaculatings\b|\bejaculation\b|\bejakulate\b|\bf u c k\b|\bf u c k e r\b|\bf4nny\b|\bfag\b|\bfagging\b|\bfaggitt\b|\bfaggot\b|\bfaggs\b|\bfagot\b|\bfagots\b|\bfags\b|\bfanny\b|\bfannyflaps\b|\bfannyfucker\b|\bfanyy\b|\bfatass\b|\bfcuk\b|\bfcuker\b|\bfcuking\b|\bfeck\b|\bfecker\b|\bfelching\b|\bfellate\b|\bfellatio\b|\bfingerfuck\b|\bfingerfucked\b|\bfingerfucker\b|\bfingerfuckers\b|\bfingerfucking\b|\bfingerfucks\b|\bfistfuck\b|\bfistfucked\b|\bfistfucker\b|\bfistfuckers\b|\bfistfucking\b|\bfistfuckings\b|\bfistfucks\b|\bflange\b|\bfook\b|\bfooker\b|\bfuck\b|\bfucka\b|\bfucked\b|\bfucker\b|\bfuckers\b|\bfuckhead\b|\bfuckheads\b|\bfuckin\b|\bfucking\b|\bfuckings\b|\bfuckingshitmotherfucker\b|\bfuckme\b|\bfucks\b|\bfuckwhit\b|\bfuckwit\b|\bfudge packer\b|\bfudgepacker\b|\bfuk\b|\bfuker\b|\bfukker\b|\bfukkin\b|\bfuks\b|\bfukwhit\b|\bfukwit\b|\bfux\b|\bfux0r\b|\bf_u_c_k\b|\bgangbang\b|\bgangbanged\b|\bgangbangs\b|\bgaylord\b|\bgaysex\b|\bgoatse\b|\bGod\b|\bgod-dam\b|\bgod-damned\b|\bgoddamn\b|\bgoddamned\b|\bhardcoresex\b|\bhell\b|\bheshe\b|\bhoar\b|\bhoare\b|\bhoer\b|\bhomo\b|\bhore\b|\bhorniest\b|\bhorny\b|\bhotsex\b|\bjack-off\b|\bjackoff\b|\bjap\b|\bjerk-off\b|\bjism\b|\bjiz\b|\bjizm\b|\bjizz\b|\bkawk\b|\bknob\b|\bknobead\b|\bknobed\b|\bknobend\b|\bknobhead\b|\bknobjocky\b|\bknobjokey\b|\bkock\b|\bkondum\b|\bkondums\b|\bkum\b|\bkummer\b|\bkumming\b|\bkums\b|\bkunilingus\b|\bl3i+ch\b|\bl3itch\b|\blabia\b|\blust\b|\blusting\b|\bm0f0\b|\bm0fo\b|\bm45terbate\b|\bma5terb8\b|\bma5terbate\b|\bmasochist\b|\bmaster-bate\b|\bmasterb8\b|\bmasterbat*\b|\bmasterbat3\b|\bmasterbate\b|\bmasterbation\b|\bmasterbations\b|\bmasturbate\b|\bmo-fo\b|\bmof0\b|\bmofo\b|\bmothafuck\b|\bmothafucka\b|\bmothafuckas\b|\bmothafuckaz\b|\bmothafucked\b|\bmothafucker\b|\bmothafuckers\b|\bmothafuckin\b|\bmothafucking\b|\bmothafuckings\b|\bmothafucks\b|\bmother fucker\b|\bmotherfuck\b|\bmotherfucked\b|\bmotherfucker\b|\bmotherfuckers\b|\bmotherfuckin\b|\bmotherfucking\b|\bmotherfuckings\b|\bmotherfuckka\b|\bmotherfucks\b|\bmuff\b|\bmutha\b|\bmuthafecker\b|\bmuthafuckker\b|\bmuther\b|\bmutherfucker\b|\bn1gga\b|\bn1gger\b|\bnazi\b|\bnigg3r\b|\bnigg4h\b|\bnigga\b|\bniggah\b|\bniggas\b|\bniggaz\b|\bnigger\b|\bniggers\b|\bnob\b|\bnob jokey\b|\bnobhead\b|\bnobjocky\b|\bnobjokey\b|\bnumbnuts\b|\bnutsack\b|\borgasim\b|\borgasims\b|\borgasm\b|\borgasms\b|\bp0rn\b|\bpawn\b|\bpecker\b|\bpenis\b|\bpenisfucker\b|\bphonesex\b|\bphuck\b|\bphuk\b|\bphuked\b|\bphuking\b|\bphukked\b|\bphukking\b|\bphuks\b|\bphuq\b|\bpigfucker\b|\bpimpis\b|\bpiss\b|\bpissed\b|\bpisser\b|\bpissers\b|\bpisses\b|\bpissflaps\b|\bpissin\b|\bpissing\b|\bpissoff\b|\bpoop\b|\bporn\b|\bporno\b|\bpornography\b|\bpornos\b|\bprick\b|\bpricks\b|\bpron\b|\bpube\b|\bpusse\b|\bpussi\b|\bpussies\b|\bpussy\b|\bpussys\b|\brectum\b|\bretard\b|\brimjaw\b|\brimming\b|\bs hit\b|\bs.o.b.\b|\bsadist\b|\bschlong\b|\bscrewing\b|\bscroat\b|\bscrote\b|\bscrotum\b|\bsemen\b|\bsex\b|\bsh!+\b|\bsh!t\b|\bsh1t\b|\bshag\b|\bshagger\b|\bshaggin\b|\bshagging\b|\bshemale\b|\bshi+\b|\bshit\b|\bshitdick\b|\bshite\b|\bshited\b|\bshitey\b|\bshitfuck\b|\bshitfull\b|\bshithead\b|\bshiting\b|\bshitings\b|\bshits\b|\bshitted\b|\bshitter\b|\bshitters\b|\bshitting\b|\bshittings\b|\bshitty\b|\bskank\b|\bslut\b|\bsluts\b|\bsmegma\b|\bsmut\b|\bsnatch\b|\bson-of-a-bitch\b|\bspac\b|\bspunk\b|\bs_h_i_t\b|\bt1tt1e5\b|\bt1tties\b|\bteets\b|\bteez\b|\btestical\b|\btesticle\b|\btit\b|\btitfuck\b|\btits\b|\btitt\b|\btittie5\b|\btittiefucker\b|\btitties\b|\btittyfuck\b|\btittywank\b|\btitwank\b|\btosser\b|\bturd\b|\btw4t\b|\btwat\b|\btwathead\b|\btwatty\b|\btwunt\b|\btwunter\b|\bv14gra\b|\bv1gra\b|\bvagina\b|\bviagra\b|\bvulva\b|\bw00se\b|\bwang\b|\bwank\b|\bwanker\b|\bwanky\b|\bwhoar\b|\bwhore\b|\bwillies\b|\bwilly\b|\bxrated\b|\bxxx\b)";
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

        private static ISocketMessageChannel LogChannel => _logChannel ?? (_logChannel = (ISocketMessageChannel)RexBotCore.Instance.RexbotClient.GetChannel(345299089975672832));

        public static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
            if (message.Length <= 2000)
                LogChannel.SendMessageAsync(message);
            else
                LogChannel.SendMessageAsync(message.Substring(0,2000));
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
                case CommandAccess.Moderator:
                    if (user.Id == RexBotCore.REXXAR_ID)
                        return true;
                    if (guilduser == null)
                        goto case CommandAccess.Rexxar;

                    if (guilduser.Roles.Any(r => r.Id == 125015001403490304ul))
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

        public static Color RandomColor()
        {
            var b = new byte[3];
            _random.NextBytes(b);
            return new Color(b[0], b[1], b[2]);
        }

        public static bool HasProfanity(string input)
        {
            return Regex.IsMatch(input, PROFANITY_REG, RegexOptions.IgnoreCase);
        }
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
            int cv;
            dic.TryGetValue(key, out cv);
            dic[key] = cv + value;
            //if (dic.ContainsKey(key))
            //    dic[key] += value;
            //else
            //    dic[key] = value;
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
            return Utilities.CTGChannels.Any(i =>
                                            {
                                                var c = RexBotCore.Instance.RexbotClient.GetChannel(i);
                                                return c.GetUser(user.Id) != null;
                                            });
        
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

        public static void AddInlineField(this EmbedBuilder em, string name, object value)
        {
            em.AddField(name, value, true);
        }
    }
}