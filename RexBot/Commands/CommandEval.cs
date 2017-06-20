using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace RexBot.Commands
{
    class CommandEval : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!eval";
        public string HelpText => "Runs the given code";
        public async Task<string> Handle(SocketMessage message)
        {
            string arg = Utilities.StripCommand(this, message.Content);
            string code = arg;
            if (arg.StartsWith("```cs"))
            {
                code = arg.Substring(5);
                code = code.Substring(0, code.Length -3);
            }

            var op = ScriptOptions.Default;
            op.AddReferences(SelectAssemblies());
            //op.AddImports("System", "System.Collections.Generic", "System.Timers", "System.Linq", "RexBot");
            var res = await CSharpScript.EvaluateAsync<object>(code,op,new Globals(message));
            GC.Collect();
            
            return res?.ToString();
            //var sc = CSharpScript.Create(code, null, typeof(Globals));
            //var res = sc.e(new Globals(message)).Result;

            //return res.ReturnValue.ToString();
        }
        private static IEnumerable<Assembly> SelectAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Where(a => !string.IsNullOrWhiteSpace(a.Location));
        }

    }
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
}
