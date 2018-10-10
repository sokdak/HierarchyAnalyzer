using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HierarchyAnalyzer
{
    public class ICSExtract
    {
        private const int MaximumDepth = 5;

        public ICSExtract()
        {

        }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <returns>The URL.</returns>
        /// <param name="baseDir">Base dir.</param>
        /// <param name="functionNames">Function names.</param>
        public static string[] GetURL(string baseDir, string functionName)
        {
            List<string> foundURLs = new List<string>();

            Console.WriteLine("[i] Finding function {0} usage", functionName);

            foundURLs.Add(string.Format(">> function call-hierarchy analysis on {0}", functionName));
            foundURLs.AddRange(CallHierarchySearch(baseDir, functionName));
            foundURLs.Add(string.Format(">> finished to analyze on {0}", functionName));

            return foundURLs.ToArray();
        }

        /// <summary>
        /// Gets the URL Containing functions.
        /// </summary>
        /// <returns>The URLC ontaining functions.</returns>
        /// <param name="args_t">Arguments t.</param>
        /// <param name="baseDir">Base dir.</param>
        public static string[] GetURLContainingFunctions(string[] args_t, string baseDir, int curDepth=-1)
        {
            List<string> functionNames = new List<string>();

            foreach (string arg in args_t)
            {
                string[] rFilenames = ICSExtract.GetBaseUrlReturningFunctionFile(arg, baseDir);

                if (rFilenames.Length > 0)
                {
                    foreach (string rFilename in rFilenames)
                    {
                        string[] funcNames = ICSExtract.주어진_함수를_호출한_메소드들_1개_파일(rFilename, arg, curDepth);

                        if (funcNames.Length > 0)
                            functionNames.AddRange(funcNames);
                    }
                }
            }

            return functionNames.ToArray();
        }

        private static string[] 문자열이_포함된_파일_찾기(string path, string pattern)
        {
            List<string> retarr = new List<string>();
            string[] files = Directory.GetFiles(path, "*.java", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                if (File.ReadAllText(file).Contains(pattern))
                    retarr.Add(file);
            }

            return retarr.ToArray();
        }

        private static string[] 주어진_함수_있는_파일_찾기(string path, string funcName, bool findDefinition=false)
        {
            List<string> foundFuncFilename = new List<string>();

            string[] files = 문자열이_포함된_파일_찾기(path, funcName);

            foreach (string file in files)
            {
                var fileFuncsContain = 주어진_파일과_함수명으로_함수_찾기(file, funcName, findDefinition);

                foreach (string fileFuncContains in fileFuncsContain)
                {
                    if (fileFuncContains != null)
                        foundFuncFilename.Add(fileFuncContains);
                }
            }

            return foundFuncFilename.ToArray();
        }

        private static string AddDepthToPrint(int depth)
        {
            string str = "";

            for (int i = 0; i < depth; i++)
                str += " ";

            return str;
        }

        private static string[] CallHierarchySearch(string baseDir, string curFuncName, int curDepth=0, string[] classNames=null, string stackedFunc=null)
        {
            List<string> foundURLs = new List<string>();

            if (stackedFunc == null)
                stackedFunc = curFuncName;

            if (curDepth > MaximumDepth)
            {
                Console.WriteLine("{0}[!:({1})] Maximum depth reached, giving up call-hierarchy search on this branch",
                                  AddDepthToPrint(curDepth),
                                  stackedFunc);
                return foundURLs.ToArray();
            }

            var referenceMethods = ICSExtract.주어진_함수를_호출한_메소드들_전체(baseDir, curFuncName, classNames);

            if (referenceMethods == null)
                return foundURLs.ToArray();
            
            foreach (var referenceMethod in referenceMethods)
            {
                string ifName = ICSExtract.파일_안에서_인터페이스_사용기록_찾기(referenceMethod.Item1); // 인터페이스 이름 리턴

                if (ifName != null) // 인터페이스 사용 기록이 있으면
                {
                    string[] files = ICSExtract.문자열이_포함된_파일_찾기(baseDir, ifName); // 인터페이스로 찾기

                    foreach (string file in files)
                    {
                        Console.WriteLine("{1}[!:({2})] found interface at depth {0}",
                                          curDepth, AddDepthToPrint(curDepth),
                                          stackedFunc);
                        foundURLs.AddRange(ICSExtract.DoMatchesFromFile(file)); // URL매치 시도
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("{2}[!:({3})] no interface usage on file {0}, dive into {1} depth",
                                      referenceMethod.Item1.Replace(baseDir, ""), curDepth,
                                      AddDepthToPrint(curDepth + 1),
                                      stackedFunc + "-" + referenceMethod.Item2);

                    foundURLs.AddRange(CallHierarchySearch(baseDir,
                                                           referenceMethod.Item2,
                                                           curDepth + 1,
                                                           referenceMethod.Item3,
                                                           stackedFunc + "-" + referenceMethod.Item2));
                }
            } // 모든 레퍼런스에 대해 인터페이스 확인

            return foundURLs.ToArray();
        }

        /// <summary>
        /// Gets the base URL retuning function file.
        /// </summary>
        /// <returns>The base URL retuning function file.</returns>
        /// <param name="baseUrl">Base URL.</param>
        /// <param name="baseDir">Base dir.</param>
        private static string[] GetBaseUrlReturningFunctionFile(string baseUrl, string baseDir)
        {
            List<string> retstr = new List<string>();
            string[] data = Directory.GetFiles(baseDir, "*.java", SearchOption.AllDirectories);

            foreach (string codefile in data) // find files on current directory
            {
                if (File.ReadAllText(codefile).Contains(baseUrl))
                {
                    //Console.WriteLine("[i] target found on file {0}", codefile);
                    retstr.Add(codefile);
                }
            }

            return retstr.ToArray();
        }

        /// <summary>
        /// Gets the name of the function.
        /// </summary>
        /// <returns>The function name.</returns>
        /// <param name="cFile">file.</param>
        /// <param name="targetContains">Target contains.</param>
        private static string[] 주어진_함수를_호출한_메소드들_1개_파일(string cFile, string targetContains, int curDepth=-1)
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

                        if (IsJavaMethodDeclarationLine(tData))
                        {
                            string funcName = ExtractJavaMethodNameFromDeclarationLine(tData);

                            if (funcName != null && !retFunctions.Exists(x => x == funcName))
                            {
                                Console.WriteLine("{2}[i] found function {0} on {1}", funcName, cFile,
                                                  curDepth > 0 ? AddDepthToPrint(curDepth) : "");
                                retFunctions.Add(funcName);
                            }
                        }
                    }
                }
            }

            return retFunctions.Distinct().ToArray();
        }

        private static bool IsJavaClassDeclarationLine(string line)
        {
            //Regex regex = new Regex(@"(?:public|protected|private|static)*\s(?:class)\s+\w+\s*\{");
            Regex regex = new Regex(@"(class)\s\w*(\s+?(extends))*");
            Match m = regex.Match(line);

            if (m.Success == true)
                return true;
            else return false;
        }

        private static bool IsJavaMethodDeclarationLine(string line)
        {
            Regex regex = new Regex(@"(public|protected|private|static|\s)(\s+final)* +[\w\<\>\[\]]+\s+(\w+) *\([^\)]*\) *(\{?|[^;])");
            Match m = regex.Match(line);

            if (m.Success == true)
                return true;
            else return false;
        }

        private static string ExtractJavaMethodNameFromDeclarationLine(string line)
        {
            Regex regex = new Regex(@"\w+\s*\(");
            Match m = regex.Match(line);

            if (m.Success)
                return m.Value.Replace("(", "");
            else return null;
        }

        private static string ExtractJavaClassNameFromDeclarationLine(string line)
        {
            //Regex regex = new Regex(@"(?!extends).\s\w+\s*(\{|extends)");
            Regex regex = new Regex(@"(class)+\s\w*(?:(\<[0-9a-zA-Z]*\>))*");
            Match m = regex.Match(line);

            if (m.Success)
            {
                string retval = m.Value.Split(' ').Last();

                if (m.Value.Contains('<'))
                    return retval.Split('<').First();
                else return retval;
            } else return null;
        }

        private static bool IsJavaMethodUsageLine(string line, string targetContains)
        {
            return (line.Contains("." + targetContains + "(")
                    &&
                    !(line.Replace(" ", "").StartsWith("private") ||
                      line.Replace(" ", "").StartsWith("public"))) ? true : false;
        }

        /// <summary>
        /// 주어진s the 함수를 호출한 메소드들 전체.
        /// </summary>
        /// <returns>item1=file, item2=functionname, item3=class</returns>
        /// <param name="baseDir">Base dir.</param>
        /// <param name="targetContains">Target contains.</param>
        private static Tuple<string, string, string[]>[] 주어진_함수를_호출한_메소드들_전체(string baseDir, string targetContains, string[] classNames=null)
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
                            if (IsJavaMethodUsageLine(lines[i], targetContains))
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
                                    //Console.WriteLine("[d] found function usage with classnames at line {0}, name: {1}, file: {2}",
                                    //                  i + 1, targetContains, file);
                                }
                            }
                        }
                        else
                        {
                            if (fline_found = IsJavaMethodUsageLine(lines[i], targetContains))
                            {
                                //    Console.WriteLine("[d] found function usage at line {0}, name: {1}, file: {2}",
                                //                      i + 1, targetContains, file);
                            }
                        }
                    }
                    else // fline을 찾으면
                    {
                        for (int j = i - 1; j > 0; j--)
                        {
                            if (!def_found)
                            {
                                if (def_found = IsJavaMethodDeclarationLine(lines[j]))
                                {
                                    string funcName = ExtractJavaMethodNameFromDeclarationLine(lines[j]);

                                    if (funcName != null)
                                    {
                                        //Console.WriteLine("[d] found method definition at line {0}, name: {1}, file: {2}",
                                        //                  j + 1, funcName, file);

                                        methodName = funcName;
                                    }
                                    else
                                    {
                                        //Console.WriteLine("[d] Cannot extract method name at line {0}, file: {1}",
                                        //                  j + 1, file);
                                        error = true;

                                        break;
                                    }
                                }

                                //if (j == 0)
                                //    Console.WriteLine("[i] cannot find method definition for {0} in {1}",
                                //                      lines[i], file);
                            }
                            else // class 찾기
                            {
                                if (!class_found) // 이미 한번 찾은 경우도 해야하므로
                                    class_found = IsJavaClassDeclarationLine(lines[j]); // 그땐 flag를 건들지 않음

                                if (IsJavaClassDeclarationLine(lines[j]))
                                {
                                    string className = ExtractJavaClassNameFromDeclarationLine(lines[j]);

                                    if (className != null)
                                    {
                                        //Console.WriteLine("[d] found class definition at line {0}, name: {1}, file: {2}",
                                        //                  j + 1, className, file);
                                        class_found = true;

                                        tmpClassNames.Add(className);
                                    }
                                    else
                                    {
                                        //Console.WriteLine("[d] Cannot extract class name at line {0}, file {1}",
                                        //                  j + 1, file);
                                        error = true;

                                        break;
                                    }
                                }

                                //if (j == 0)
                                //    Console.WriteLine("[i] cannot find class definition containing {0} in {1}",
                                //                     lines[i], file);
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

        /// <summary>
        /// Gets the name of the function usage file.
        /// </summary>
        /// <returns>The function usage file name.</returns>
        /// <param name="cFile">File</param>
        /// <param name="funcName">Func name.</param>
        private static string[] 주어진_파일과_함수명으로_함수_찾기(string cFile, string funcName, bool findDefinition=false)
        {
            List<string> retStr = new List<string>();

            using (StreamReader sr = new StreamReader(cFile))
            {
                var tdata = sr.ReadLine();
                var line = 1;

                while (tdata != null)
                {
                    if (tdata.Contains(funcName))
                    {
                        //Console.WriteLine(" [i] found function(name: {0}) usage from {1} at line {2}",
                        //                    funcName, cFile, line);

                        if (!findDefinition)
                        {
                            retStr.Add(cFile);
                            break;
                        }
                        else
                        {
                            if (tdata.Contains("public") ||
                               tdata.Contains("private") ||
                               tdata.Contains("internal") &&
                               !tdata.Contains("return"))
                            {
                                retStr.Add(cFile);
                                break;
                            }
                        }
                    }

                    tdata = sr.ReadLine();
                    line++;
                }
            }

            return retStr.ToArray();
        }

        /// <summary>
        /// Gets the name of the interface from file.
        /// </summary>
        /// <returns>The interface from file name.</returns>
        /// <param name="cFile">file.</param>
        private static string 파일_안에서_인터페이스_사용기록_찾기(string cFile)
        {
            string retStr = null;

            using (StreamReader sr = new StreamReader(cFile))
            {
                var tdata = sr.ReadLine();
                var line = 1;

                string interfaceString = "";

                while (tdata != null)
                {
                    tdata = tdata.Trim();

                    if (tdata.StartsWith("this.") &&
                        tdata.EndsWith(".class);"))
                    {
                        string ddata = tdata.Split('(').Last();
                        interfaceString = ddata.Split(')').First().Split(".class").First();

                        //Console.WriteLine("  [i] found interface(name: {0}) at line {1}",
                        //                  interfaceString, line);

                        retStr = "interface " + interfaceString;
                        break;
                    }

                    tdata = sr.ReadLine();
                    line++;
                }
            }

            return retStr;
        }

        /// <summary>
        /// Does the regex matches from file.
        /// </summary>
        /// <returns>The regex matches from file.</returns>
        /// <param name="cFile">C file.</param>
        private static string[] DoMatchesFromFile(string cFile)
        {
            List<string> matchedString = new List<string>();

            using (StreamReader sr = new StreamReader(cFile))
            {
                var tdata = sr.ReadLine();
                var line = 1;

                while (tdata != null)
                {
                    if (tdata.Replace(" ", "").StartsWith("@") &&
                        tdata.Replace(" ", "").EndsWith("\")") &&
                        tdata.Count(x => x.ToString() == "\"") == 2)
                    {
                        matchedString.Add(tdata.Trim());
                        matchedString.Add(sr.ReadLine().Trim());
                    }

                    tdata = sr.ReadLine();
                    line++;
                }
            }

            if (matchedString.Count > 0)
                Console.WriteLine("[!] {1} lines of matched found at: {0}", cFile, matchedString.Count);
            else Console.WriteLine("[!] no lines matched at: {0}", cFile);

            return matchedString.ToArray();
        }
    }
}
