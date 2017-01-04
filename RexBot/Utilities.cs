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
            var matches = Regex.Matches( input, "(\"[^\"]+\"|\\S+)" );
            string[] result = new string[matches.Count - 1];
            for (int i = 0; i < result.Length; i++)
                result[i] = matches[i + 1].Value;
            return result;
        }
    }
}
