using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Inedo.BuildMasterExtensions.DotNetRecipes
{
    internal static class Replacer
    {
        public static string Replace(string text, Dictionary<string, string> values)
        {
            return Regex.Replace(
                text,
                @"\$\[(?<1>[^\]\r\n]+)\]",
                m => values.GetValueOrDefault(m.Groups[1].Value, m.Value),
                RegexOptions.ExplicitCapture
            );
        }
    }
}
