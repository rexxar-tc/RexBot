using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RexBot
{
    public static class Utilities
    {
        public static string[] ParseCommand( string input )
        {
            var splits = Regex.Split( input, "(\"[^\"]+\"|\\S+)" );
            string[] result = new string[splits.Length -1];
            Array.Copy( splits, 1, result, 0, result.Length );
            return result;
        }
    }
}
