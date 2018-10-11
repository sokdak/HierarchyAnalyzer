using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HierarchyAnalyzer
{
    public class Analyzer
    {
        private const string TAG = "Analyzer";

        private int MaximumDepth { get; set; }
        public string BaseDir { get; private set; }

        public Analyzer(string baseDir, int maximumDepth=5)
        {
            this.BaseDir = baseDir;
            this.MaximumDepth = maximumDepth;
        }

        public string[] GetURI(string[] relatedURL)
        {
            List<string> foundURLs = new List<string>();

            string[] functionNames = this.GetURLContainingFunctions(relatedURL);

            foreach (string funcName in functionNames)
            {
                Console.WriteLine("[i] Finding function {0} usage", funcName);

                foundURLs.Add(string.Format(" - function call-hierarchy analysis on {0}", funcName));
                foundURLs.AddRange(CallHierarchySearch(BaseDir, funcName));
                foundURLs.Add(string.Format(" - finished to analyze on {0}", funcName));
            }

            return foundURLs.ToArray();
        }

        public string[] GetURLContainingFunctions(string[] urls, int curDepth=-1)
        {
            List<string> functionNames = new List<string>();

            foreach (string url in urls)
            {
                string[] rFilenames = IO.GetBaseUrlReturningFunctionFile(url, BaseDir);

                if (rFilenames.Length > 0)
                {
                    foreach (string rFilename in rFilenames)
                    {
                        string[] funcNames = Analyzer.FindUsageOnSingleFile(rFilename, url, curDepth);

                        if (funcNames.Length > 0)
                            functionNames.AddRange(funcNames);
                    }
                }
            }

            return functionNames.ToArray();
        }

        private string[] CallHierarchySearch(string currentDir, string curFuncName, int curDepth=0, string[] classNames=null, string stackedFunc=null)
        {
            List<string> foundURLs = new List<string>();

            if (stackedFunc == null)
                stackedFunc = curFuncName;

            if (curDepth > this.MaximumDepth)
            {
                Console.WriteLine("{0}[!:({1})] Maximum depth reached, giving up call-hierarchy search on this branch",
                                  Logger.AddDepthToPrint(curDepth),
                                  stackedFunc);
                return foundURLs.ToArray();
            }

            var usages = Analyzer.FindUsages(currentDir, curFuncName, classNames, curDepth);

            if (usages == null)
                return foundURLs.ToArray();
            
            foreach (var usage in usages)
            {
                string ifName = Matcher.FindInterfaceNameOnFile(usage.Item1);

                if (ifName != null)
                {
                    string[] files = IO.FindPatternContainingFile(currentDir, ifName);

                    foreach (string file in files)
                    {
                        Console.WriteLine("{1}[!:({2})] found interface at depth {0}",
                                          curDepth, Logger.AddDepthToPrint(curDepth),
                                          stackedFunc);
                        foundURLs.AddRange(Matcher.DoMatchesFromFile(file, curDepth));
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("{2}[!:({3})] no interface usage on file {0}, dive into {1} depth",
                                      usage.Item1.Replace(currentDir, ""), curDepth,
                                      Logger.AddDepthToPrint(curDepth + 1),
                                      stackedFunc + "-" + usage.Item2);

                    foundURLs.AddRange(CallHierarchySearch(currentDir,
                                                           usage.Item2,
                                                           curDepth + 1,
                                                           usage.Item3,
                                                           stackedFunc + "-" + usage.Item2));
                }
            }

            return foundURLs.ToArray();
        }

        private static string[] FindUsageOnSingleFile(string cFile, string targetContains, int curDepth=-1)
        {
            List<string> retFunctions = new List<string>();
            List<int> foundLines = new List<int>();

            string[] cData = File.ReadAllLines(cFile);

            for (int j = 0; j < cData.Length; j++)
            {
                int foundTarget = -1;

                for (int i = 0; i < cData.Length; i++)
                {
                    if (cData[i].Contains(targetContains) &&
                        !foundLines.Exists(x => x.Equals(i)))
                    {
                        foundLines.Add(i);
                        foundTarget = i;

                        break;
                    }
                }

                if (foundTarget != -1)
                {
                    for (int i = foundTarget; i > 0; i--) // find function's header
                    {
                        string tData = cData[i].Trim();

                        if (Matcher.IsJavaMethodDeclarationLine(tData))
                        {
                            string funcName = Parser.ExtractJavaMethodNameFromDeclarationLine(tData);

                            if (funcName != null && !retFunctions.Exists(x => x == funcName))
                            {
                                Console.WriteLine("{2}[i] found function {0} on {1}", funcName, cFile,
                                                  curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");
                                retFunctions.Add(funcName);
                            }
                        }
                    }
                }
            }

            return retFunctions.Distinct().ToArray();
        }

        private static Tuple<string, string, string[]>[] FindUsages(string baseDir, string targetContains, string[] classNames=null, int curDepth=-1)
        {
            var retmethods = new List<Tuple<string, string, string[]>>();

            string[] usageFound = Directory.GetFiles(baseDir, "*.java", SearchOption.AllDirectories);

            foreach (string file in usageFound)
            {
                string[] lines = File.ReadAllLines(file);

                var tmpClassNames = new List<string>();

                bool fline_found = false;
                bool def_found = false;
                bool class_found = false;

                bool error = false;

                string methodName = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (!fline_found)
                    {
                        if (classNames != null)
                        {
                            if (Matcher.IsJavaMethodUsageLine(lines[i], targetContains))
                            {
                                int count = 0;

                                foreach (string className in classNames)
                                {
                                    if (lines[i].Contains(className))
                                        count++;
                                }

                                if (count > 0)
                                {
                                    fline_found = true;
                                    Console.WriteLine("{3}[d] found function usage with classnames at line {0}, name: {1}, file: {2}",
                                                      i + 1, targetContains, file,
                                                      curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");
                                }
                            }
                        }
                        else
                        {
                            if (fline_found = Matcher.IsJavaMethodUsageLine(lines[i], targetContains))
                            {
                                Console.WriteLine("{3}[d] found function usage at line {0}, name: {1}, file: {2}",
                                                  i + 1, targetContains, file,
                                                  curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");
                            }
                        }
                    }
                    else // fline을 찾으면
                    {
                        for (int j = i - 1; j > 0; j--)
                        {
                            if (!def_found)
                            {
                                if (def_found = Matcher.IsJavaMethodDeclarationLine(lines[j]))
                                {
                                    string funcName = Parser.ExtractJavaMethodNameFromDeclarationLine(lines[j]);

                                    if (funcName != null)
                                    {
                                        Console.WriteLine("{3}[d] found method definition at line {0}, name: {1}, file: {2}",
                                                          j + 1, funcName, file,
                                                          curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");

                                        methodName = funcName;
                                    }
                                    else
                                    {
                                        Console.WriteLine("{2}[d] Cannot extract method name at line {0}, file: {1}",
                                                          j + 1, file,
                                                          curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");

                                        error = true;

                                        break;
                                    }
                                }

                                if (j == 0)
                                    Console.WriteLine("{2}[i] cannot find method definition for {0} in {1}",
                                                      lines[i], file,
                                                      curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");
                            }
                            else // class 찾기
                            {
                                if (!class_found) // 이미 한번 찾은 경우도 해야하므로
                                    class_found = Matcher.IsJavaClassDeclarationLine(lines[j]); // 그땐 flag를 건들지 않음

                                if (Matcher.IsJavaClassDeclarationLine(lines[j]))
                                {
                                    string className = Parser.ExtractJavaClassNameFromDeclarationLine(lines[j]);

                                    if (className != null)
                                    {
                                        Console.WriteLine("{3}[d] found class definition at line {0}, name: {1}, file: {2}",
                                                          j + 1, className, file,
                                                          curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");

                                        class_found = true;

                                        tmpClassNames.Add(className);
                                    }
                                    else
                                    {
                                        Console.WriteLine("{2}[d] Cannot extract class name at line {0}, file {1}",
                                                          j + 1, file,
                                                          curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");
                                        error = true;

                                        break;
                                    }
                                }

                                if (j == 0)
                                    Console.WriteLine("{2}[i] cannot find class definition containing {0} in {1}",
                                                       lines[i], file,
                                                       curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");
                            }
                        }

                        if (class_found || error)
                            break;
                    }
                }

                if (class_found)
                    retmethods.Add(new Tuple<string, string, string[]>(file, methodName, tmpClassNames.ToArray()));
            }

            return retmethods.Count > 0 ? retmethods.ToArray() : null;
        }
    }
}
