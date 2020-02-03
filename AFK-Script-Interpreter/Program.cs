using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using ArgumentsUtil;

namespace AFK_Script_Interpreter
{
    class Program
    {
        private static bool childProcesses;
        
        static void Main(string[] args)
        {
            // args = new[] {@"Examples\logging.afk", @"Examples\movie time.afk", @"Examples\program.afk", "/cp"};
            args = new[] {@"Examples\movie time.afk", @"Examples\program.afk", "/cp"};

            Arguments a = Arguments.Parse(args);
            childProcesses = a.ContainsKey("cp");
            if (a.Keyless.Count > 0)
            {
                string file = a.Keyless[0];
                // Run the first script here
                if (!File.Exists(file))
                {
                    Console.WriteLine($"The script file '{file}' was not found!");
                }
                else
                {
                    if (childProcesses)
                    {
                        Thread t = new Thread (new ParameterizedThreadStart(Run));
                        t.Start(file);
                    }
                    else
                    {
                        Run(file);
                    }
                }
                // Start all other scripts as new processes
                if (a.Keyless.Count > 1)
                {
                    for (int i = 1; i < a.Keyless.Count; i++)
                    {
                        file = a.Keyless[i];
                        if (!File.Exists(file))
                        {
                            Console.WriteLine($"The script file '{file}' was not found!");
                            continue;
                        }

                        if (childProcesses)
                        {
                            Thread t = new Thread (new ParameterizedThreadStart(Run));
                            t.Start(file);
                        }
                        else Process.Start(Assembly.GetExecutingAssembly().Location, file);
                    }
                }
            }
            else
            {
                ArgumentsTemplate at = new ArgumentsTemplate(new List<ArgumentOption>()
                {
                    new ArgumentOption("cp", "children", "Run all (if multiple) scripts as child processes")
                }, false, new List<ArgumentCommand>()
                {
                    new ArgumentCommand("file(s)", "The AFK Script file(s) to execute")
                }, true, null, "AFK Script", (char)KeySelector.CrossPlatformCompatible);
                at.ShowManual();
            }
        }
        
        static void Run(object parameters)
        {
            string filepath = parameters.ToString();
            string filename = Path.GetFileName(filepath);
            string rawfilename = Path.GetFileNameWithoutExtension(filepath);
            Dictionary<string, string> localVariables = new Dictionary<string, string>();

            // Check filetype
            if (Path.GetExtension(filepath).ToLower() != ".afk")
            {
                Console.WriteLine($"The file {filename} is not a valid AFK Script file...");
            }

            string[] instructions = File.ReadAllLines(filepath);
            for (int i = 0; i < instructions.Length; i++)
            {
                string[] instructionParts = Regex.Matches(instructions[i].Split('#')[0], "(\"[^\"\\n]+\")|([^\\x20\\t\\n]+)", RegexOptions.Compiled).Cast<Match>().Select(
                    p => p.Value
                ).ToArray();

                string instrcutionName = instructionParts.Length > 0 ? instructionParts[0].ToUpper() : "";
                int instructionArgsCount = instructionParts.Length - 1;
                string instructionArgs(int index) => SubstituteVariables(instructionParts[index + 1], localVariables).TrimStart('"').TrimEnd('"');
                string instructionArgsRaw(int index) => instructionParts[index + 1].TrimStart('"').TrimEnd('"');
                string instructionArgsRawest(int index) => instructionParts[index + 1]; // Don't ask

                if (instrcutionName == "") continue;
                switch (instrcutionName)
                {
                    case "AT":

                        DateTime date = DateTime.Now;
                        if (instructionArgsCount == 1)      // DATE or TIME
                        {
                            date = DateTime.Parse(instructionArgs(0),
                                System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else if (instructionArgsCount == 2) // DATE and TIME
                        {
                            date = DateTime.Parse(instructionArgs(0) + " " + instructionArgs(1),
                                System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Error_TooManyParams(instrcutionName, "1 or 2", filename);
                            break;
                        }

                        
                        string waitingMessage = $"Waiting for '{date}' to pass by...";
                        if (childProcesses) Console.WriteLine(rawfilename + ": " + waitingMessage);
                        else Console.WriteLine(waitingMessage);
                        

                        while (DateTime.Now.CompareTo(date) < 0)
                        {
                            // Make this more efficient
                            Thread.Sleep(1000);
                        }
                        break;
                    case "LOG":
                        if (instructionArgsCount == 1)
                        {
                            string text = instructionArgs(0);
                            if (childProcesses) Console.WriteLine(rawfilename + ": " + text);
                            else Console.WriteLine(text);
                        }
                        else
                        {
                            Error_TooManyParams(instrcutionName, 1, filename);
                            break;
                        }
                        break;
                    case "READ":
                        if (instructionArgsCount == 1)
                        {
                            string variable = instructionArgsRaw(0);
                            if (variable.StartsWith("$"))
                            {
                                string value = Console.ReadLine();
                                if (!localVariables.ContainsKey(variable)) localVariables.Add(variable, value);
                                else localVariables[variable] = value;
                            }
                            else
                            {
                                Error_UnexpectedToken(variable, "variable name", filename);
                            }
                        }
                        else if (instructionArgsCount == 2)
                        {
                            string text = instructionArgs(0);
                            string variable = instructionArgsRaw(1);
                            if (variable.StartsWith("$"))
                            {
                                Console.Write(text);
                                string value = Console.ReadLine();
                                if (!localVariables.ContainsKey(variable)) localVariables.Add(variable, value);
                                else localVariables[variable] = value;
                            }
                            else
                            {
                                Error_UnexpectedToken(variable, "variable name", filename);
                            }
                        }
                        else
                        {
                            Error_TooManyParams(instrcutionName, "[text] [variable] or [variable]", filename);
                            break;
                        }
                        break;
                    case "START":
                        if (instructionArgsCount > 0)
                        {
                            string program = instructionArgs(0);
                            string arguments = "";

                            for (int j = 1; j < instructionArgsCount; j++) arguments += $"\"{instructionArgs(j)}\" ";

                            if (program.Trim() == string.Empty) Process.Start(arguments.TrimEnd());
                            else Process.Start(program, arguments.TrimEnd());
                        }
                        else
                        {
                            Error_TooFewParams(instrcutionName, "[program] (argument1) (argument2) ...", filename);
                            break;
                        }
                        break;
                    case "PAUSE":
                        Console.ReadKey(true);
                        break;
                    case "WAIT":
                        if (instructionArgsCount == 1)
                        {
                            string durationRaw = instructionArgsRaw(0);
                            int duration;
                            if (int.TryParse(durationRaw, out duration))
                            {
                                Thread.Sleep(duration);
                            }
                            else
                            {
                                Error_UnexpectedToken(durationRaw, "[milliseconds]", filename);
                            }
                        }
                        else
                        {
                            Error_TooManyParams(instrcutionName, "[milliseconds]", filename);
                            break;
                        }
                        break;
                    default:
                        Error_UnknownInstruction(instrcutionName, i, filename);
                        break;
                }
            }
        }

        static Dictionary<string, string> EnvironmentVariables = new Dictionary<string, string>()
        {
            {"$TIME", DateTime.Now.ToString("HH:mm:ss")},
            {"$DATE", DateTime.Now.ToString("MM'/'dd'/'yyyy")},
            {"$TEST", "Hello, this is a general test :D"}
        };
        static string SubstituteVariables(string input, Dictionary<string, string> variables)
        {
            if (!input.Contains('$') && input.Length > 1) return input;

            // Environment Variables
            for (int i = 0; i < EnvironmentVariables.Count; i++)
            {
                string varkey = EnvironmentVariables.ElementAt(i).Key;
                while (input.Contains(varkey))
                {
                    input = input.Replace(varkey, EnvironmentVariables[varkey]);
                }
            }

            // Variables found
            for (int i = 0; i < variables.Count; i++)
            {
                string varkey = variables.ElementAt(i).Key;
                while (input.Contains(varkey))
                {
                    input = input.Replace(varkey, variables[varkey]);
                }
            }

            return input;
        }

        static void Error_UnknownInstruction(string instruction, int line, string file)
        {
            if (childProcesses) Console.Write($"Error in {file}: ");
            Console.WriteLine($"Unknown instruction '{instruction}' at line {line + 1}!");
        }
        static void Error_TooManyParams(string instruction, object expected, string file)
        {
            if (childProcesses) Console.Write($"Error in {file}: ");
            Console.WriteLine($"Too many parameters for instruction {instruction}! Expected {expected}...");
        }
        static void Error_TooFewParams(string instruction, object expected, string file)
        {
            if (childProcesses) Console.Write($"Error in {file}: ");
            Console.WriteLine($"Too few parameters for instruction {instruction}! Expected {expected}...");
        }
        static void Error_UnexpectedToken(string token, object expected, string file)
        {
            if (childProcesses) Console.Write($"Error in {file}: ");
            Console.WriteLine($"Unexpected token {token}! Expected {expected}...");
        }
    }
}
