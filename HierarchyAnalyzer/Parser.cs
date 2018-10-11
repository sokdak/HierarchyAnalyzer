using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace HierarchyAnalyzer
{
    internal class Parser
    {
        private const string TAG = "Parser";

        internal static string ExtractJavaMethodNameFromDeclarationLine(string line)
        {
            Regex regex = new Regex(@"\w+\s*\(");
            Match m = regex.Match(line);

            if (m.Success)
                return m.Value.Replace("(", "");
            else return null;
        }

        internal static string ExtractJavaClassNameFromDeclarationLine(string line)
        {
            Regex regex = new Regex(@"(class)+\s\w*(?:(\<[0-9a-zA-Z]*\>))*");
            Match m = regex.Match(line);

            if (m.Success)
            {
                string retval = m.Value.Split(' ').Last();

                if (m.Value.Contains('<'))
                    return retval.Split('<').First();
                else return retval;
            }
            else return null;
        }
    }
}
