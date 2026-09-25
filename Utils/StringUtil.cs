using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Physics_Items.Utils
{
    internal class StringUtil
    {
        public static string SanitizeString(ref string str)
        {
            List<char> invalidChars = new List<char>()
            {
                '\t',
                '=',
                '\n',
                '\\',
                '"',
                '\'',
                '[',
                ']'
            };
            foreach (char c in invalidChars)
            {
                str = str.Replace(c.ToString(), "");
            }
            return str;
        }
    }
}
