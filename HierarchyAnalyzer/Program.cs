using System;
using System.IO;

namespace HierarchyAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = "/Users/sokdak/Desktop/대검/nugu_apk_jadxed";
            string resSavePath = "/Users/sokdak/Desktop/res.txt";

            string[] args_t = { "sktnugu.com" };
            string[] functionNames = ICSExtract.GetURLContainingFunctions(args_t, baseDir);

            using (StreamWriter sw = new StreamWriter(resSavePath, false))
            {
                foreach (string funcName in functionNames)
                {
                    string[] res = ICSExtract.GetURL(baseDir, funcName);

                    foreach (string b in res)
                        sw.WriteLine(b);
                }

                sw.WriteLine(">> batch finished");
            }

            Console.WriteLine("[i] completed.");
        }
    }
}
