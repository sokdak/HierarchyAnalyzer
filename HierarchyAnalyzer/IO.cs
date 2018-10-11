using System;
using System.Collections.Generic;
using System.IO;

namespace HierarchyAnalyzer
{
    internal class IO
    {
        private const string TAG = "IO";

        internal static bool writeToFile(string str, string path, bool append)
        {
            bool error = false;

            try
            {
                using (StreamWriter sw = new StreamWriter(path, append))
                {
                    sw.WriteLine(str);
                }
            }
            catch (Exception e)
            {
                error = true;
            }

            return error ? false : true;
        }

        private static string[] GetAllFilesFromBaseDirectoryByExtension(string baseDir, string extension)
        {
            return Directory.GetFiles(baseDir, extension, SearchOption.AllDirectories);
        }

        internal static string[] FindPatternContainingFile(string path, string pattern)
        {
            List<string> retarr = new List<string>();
            string[] files = GetAllFilesFromBaseDirectoryByExtension(path, "*.java");

            foreach (string file in files)
            {
                if (File.ReadAllText(file).Contains(pattern))
                    retarr.Add(file);
            }

            return retarr.ToArray();
        }

        internal static string[] GetBaseUrlReturningFunctionFile(string baseStr, string baseDir)
        {
            List<string> retstr = new List<string>();
            string[] data = GetAllFilesFromBaseDirectoryByExtension(baseDir, "*.java");

            foreach (string codefile in data) // find files on current directory
            {
                if (File.ReadAllText(codefile).Contains(baseStr))
                {
                    //Console.WriteLine("[i] target found on file {0}", codefile);
                    retstr.Add(codefile);
                }
            }

            return retstr.ToArray();
        }
    }
}
