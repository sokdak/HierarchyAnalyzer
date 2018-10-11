using System;

namespace HierarchyAnalyzer
{
    internal class Logger
    {
        private const string TAG = "Logger";

        private static Logger _instance = null;

        internal bool FileWrite { get; private set; }
        internal string DefaultWritePath { get; set; }
        internal bool AlreadyWrittenOnce { get; private set; }

        public static Logger GetInstance
        {
            get
            {
                if (_instance == null)
                    _instance = new Logger();

                return _instance;
            }
        }

        private Logger(bool fileWrite=false, string writePath=null)
        {
            FileWrite = fileWrite;
            DefaultWritePath = writePath;
            AlreadyWrittenOnce = false;
        }

        internal static string AddDepthToPrint(int depth)
        {
            string str = "";

            for (int i = 0; i < depth; i++)
                str += " ";

            return str;
        }

        private void LogToFile(string str)
        {
            IO.writeToFile(str, DefaultWritePath, AlreadyWrittenOnce);

            AlreadyWrittenOnce = true;
        }

        internal void Debug(string dbgString, params string[] paramStrings)
        {
            Console.WriteLine(dbgString, paramStrings);
        }
    }
}
