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

            Analyzer icse = new Analyzer(baseDir, 5);

            using (StreamWriter sw = new StreamWriter(resSavePath, false))
            {
                sw.WriteLine("> batch started for {0}", baseDir);

                var res = icse.GetURI(args_t);

                foreach (var line in res)
                    sw.WriteLine(line);
                
                sw.WriteLine(">> batch finished");
            }

            Console.WriteLine("[i] completed.");
        }
    }
}
