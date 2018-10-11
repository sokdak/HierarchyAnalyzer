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

            var functionNames = GetURLContainingFunctions(relatedURL);

            foreach (var funcName in functionNames)
            {
                Console.WriteLine("[i] Finding function {0} usage", funcName.Item1);

                foundURLs.Add(string.Format(" - function call-hierarchy analysis on {0}, url: {1}",
                                            funcName.Item1, funcName.Item2));
                foundURLs.AddRange(CallHierarchySearch(BaseDir, funcName.Item1));
                foundURLs.Add(string.Format(" - finished to analyze on {0}", funcName));
            }

            return foundURLs.ToArray();
        }

        public Tuple<string, string>[] GetURLContainingFunctions(string[] urls, int curDepth=-1)
        {
            var functionNames = new List<Tuple<string, string>>();

            foreach (string url in urls)
            {
                string[] rFilenames = IO.GetBaseUrlReturningFunctionFile(url, BaseDir);

                if (rFilenames.Length > 0)
                {
                    foreach (string rFilename in rFilenames)
                    {
                        var funcNames = Analyzer.FindUsageOnSingleFile(rFilename, url, curDepth);

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

            Console.WriteLine("{0}[i:({1})] Found {2} of usages at method {3}",
                              curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "",
                              stackedFunc,
                              usages != null ? usages.Count() : 0,
                              curFuncName);

            if (usages == null)
                return foundURLs.ToArray();
            
            foreach (var usage in usages)
            {
                string ifName = Matcher.FindInterfaceNameOnFile(usage.Item1, curDepth);

                if (ifName != null)
                {
                    string[] files = IO.FindPatternContainingFile(currentDir, ifName);

                    foreach (string file in files)
                    {
                        Console.WriteLine("{1}[!:({2})] found {3} at depth {0}",
                                          curDepth, Logger.AddDepthToPrint(curDepth),
                                          stackedFunc,
                                          ifName);

                        foundURLs.Add(string.Format("  - Found interface: {0}",
                                                    file));

                        var res1 = Matcher.DoMatchesFromFile(file, curDepth);

                        foreach (var res in res1)
                        {
                            foundURLs.Add(string.Format("{3}\t{0}\t{1}\t{2}",
                                                        res.Item1, res.Item2, res.Item3,
                                                        ifName.Replace("interface ", "")));
                        }

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

        private static Tuple<string, string>[] FindUsageOnSingleFile(string cFile, string targetContains, int curDepth=-1)
        {
            var retFunctions = new List<Tuple<string, string>>(); // func, url
            var foundLines = new List<int>();
            var foundUrls = new List<string>();

            string[] cData = File.ReadAllLines(cFile);

            string urlTmp = "";

            for (int i = 0; i < cData.Length; i++)
            {
                if (cData[i].Contains(targetContains) &&
                    !foundLines.Exists(x => x == i))
                {
                    urlTmp = Parser.ExtractURL(cData[i]);

                    foundUrls.Add(urlTmp);
                    foundLines.Add(i);
                }
            }

            if (foundLines.Any())
            {
                for (int j = 0; j < foundLines.Count(); j++)
                {
                    for (int i = foundLines[j]; i > 0; i--) // find function's header
                    {
                        string tData = cData[i].Trim();

                        if (Matcher.IsJavaMethodDeclarationLine(tData))
                        {
                            string funcName = Parser.ExtractJavaMethodNameFromDeclarationLine(tData);

                            if (funcName != null)
                            {
                                Console.WriteLine("{2}[i] found function {0} on {1}", funcName, cFile,
                                                  curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");

                                retFunctions.Add(new Tuple<string, string>(funcName, foundUrls[j]));
                                break;
                            }
                        }
                    }
                }
            }

            // url distinct
            Tuple<string, string> tmp = null;
            var ntmp = new List<Tuple<string, string>>();

            foreach (var a in retFunctions)
            {
                if (tmp == null)
                {
                    tmp = a;
                    ntmp.Add(a);

                    continue;
                }
                else
                {
                    if (tmp.Item1 != a.Item1)
                        ntmp.Add(a);
                }

                tmp = a;
            }

            return ntmp.ToArray();
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
                                    //Console.WriteLine("{3}[d] found function usage with classnames at line {0}, name: {1}, file: {2}",
                                    //                  i + 1, targetContains, file,
                                    //                  curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");
                                }
                            }
                        }
                        else
                        {
                            if (fline_found = Matcher.IsJavaMethodUsageLine(lines[i], targetContains))
                            {
                                //Console.WriteLine("{3}[d] found function usage at line {0}, name: {1}, file: {2}",
                                //                  i + 1, targetContains, file,
                                //                  curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");
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
                                        //Console.WriteLine("{3}[d] found method definition at line {0}, name: {1}, file: {2}",
                                        //                  j + 1, funcName, file,
                                        //                  curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");

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
                                        //Console.WriteLine("{3}[d] found class definition at line {0}, name: {1}, file: {2}",
                                        //                  j + 1, className, file,
                                        //                  curDepth > 0 ? Logger.AddDepthToPrint(curDepth) : "");

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
