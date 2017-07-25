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
using Discord;
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
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            string arg = Utilities.StripCommand(this, message.Content);
            string code = arg;
            if (arg.StartsWith("```cs"))
            {
                code = arg.Substring(5);
                code = code.Substring(0, code.Length -3);
            }

            try
            {
                var res = await ScriptManager.ExecuteScript(code, message);
                return res.ToString();
            }
            catch (CompilationErrorException ex)
            {
                return $"Error executing script!" +
                       $"```" +
                       $"{ex.Message}```";
            }
            //var sc = CSharpScript.Create(code, null, typeof(Globals));
            //var res = sc.e(new Globals(message)).Result;

            //return res.ReturnValue.ToString();
        }

    }
}
