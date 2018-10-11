using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace HierarchyAnalyzer
{
    internal class Parser
    {
        private const string TAG = "Parser";

        private static string RegexMatchInternal(string regexPattern, string line)
        {
            Regex regex = new Regex(regexPattern);
            Match match = regex.Match(line);

            return match.Success ? match.Value : null;
        }

        internal static string ExtractJavaMethodNameFromDeclarationLine(string line)
        {
            var ret = RegexMatchInternal(@"\w+\s*\(", line);

            if (ret != null)
                return ret.Replace("(", "");
            else return null;
        }

        internal static string ExtractJavaClassNameFromDeclarationLine(string line)
        {
            var ret = RegexMatchInternal(@"(class)+\s\w*(?:(\<[0-9a-zA-Z]*\>))*", line);

            if (ret != null)
            {
                string retval = ret.Split(' ').Last();

                if (retval.Contains('<'))
                    return retval.Split('<').First();
                else return retval;
            }
            else return null;
        }

        internal static string ExtractURIFromMetadataDeclarationLine(string line)
        {
            return RegexMatchInternal("\"(\\w*.+(?:/)*)\"", line);
        }

        internal static string ExtractReferenceClassTypeDeclarationLine(string line)
        {
            var ret = RegexMatchInternal(@"\<\w*\>", line);

            if (ret != null)
                return ret.Replace("<", "").Replace(">", "");
            else return null;
        }

        internal static string ExtractReferenceCallingMethodDeclarationLine(string line)
        {
            return RegexMatchInternal(@"\w*\(+(.+)*\)", line);
        }

        internal static string ExtractURL(string line)
        {
            return RegexMatchInternal("\"((https|http):\\/\\/.+)\"", line);
        }
    }
}