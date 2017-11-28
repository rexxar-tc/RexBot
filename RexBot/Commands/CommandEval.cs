using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis.Scripting;

namespace RexBot.Commands
{
    class CommandEval : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!eval";
        public string HelpText => "Runs the given code";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
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
