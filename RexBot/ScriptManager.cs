using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RexBot.Commands;

namespace RexBot
{
    public class Globals
    {
        public SocketMessage Message;
        public RexBotCore BotCore;

        public Globals(SocketMessage message)
        {
            Message = message;
            BotCore = RexBotCore.Instance;
        }
    }

    public static class ScriptManager
    {
        public static async Task<object> ExecuteScript(string code, SocketMessage message = null)
        {
            var op = ScriptOptions.Default
                                  .WithReferences("System.Runtime, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b03f5f7f11d50a3a")
                                  .WithReferences(typeof(List<>).Assembly, typeof(Enumerable).Assembly, typeof(string).Assembly, typeof(RexBotCore).Assembly, typeof(StringBuilder).Assembly)
                                  .WithImports("System", "System.Collections.Generic", "System.Timers", "System.Linq", "System.Text", "RexBot");

            var res = await CSharpScript.EvaluateAsync<object>(code, op, new Globals(message));
            GC.Collect();

            return res?.ToString();
        }

        private static IEnumerable<Assembly> SelectAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Where(a => !string.IsNullOrWhiteSpace(a.Location));
        }
    }
}
