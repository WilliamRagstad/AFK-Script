using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArgumentsUtil;

namespace AFK_Script_Interpreter
{
    class Program
    {
        private static bool childProcesses;
        private static bool hideLogging;
        private static Cursor Cursor;

        static void Main(string[] args)
        {
            // args = new[] {@"Examples\logging.afk", @"Examples\movie time.afk", @"Examples\program.afk", "/cp"};
            //args = new[] {@"Examples\movie time.afk", @"Examples\program.afk", "/cp"};
            // args = new[] {@"Examples\logging.afk", "/cp", "/hl"};

            Arguments a = Arguments.Parse(args);
            childProcesses = a.ContainsKey("cp");
            hideLogging = a.ContainsKey("hl");
            Cursor = new Cursor(Cursor.Current.Handle);

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
                try
                {

                    ArgumentsTemplate at = new ArgumentsTemplate(new List<ArgumentOption>()
                    {
                        new ArgumentOption("cp", "Run all (if multiple) scripts as child processes"),
                        new ArgumentOption("hl", "Hide logging")
                    }, false, new List<ArgumentCommand>()
                    {
                        new ArgumentCommand("file(s)", "The AFK Script file(s) to execute")
                    }, true, new List<ArgumentText>(), "AFK Script", (char)KeySelector.CrossPlatformCompatible);
                    at.ShowManual();
                }
                catch
                {
                    Console.WriteLine("Could not show command line");
                }
            }
        }

        static void Run(object parameters)
        {
            string filepath = parameters.ToString();
            string filename = Path.GetFileName(filepath);
            string rawfilename = Path.GetFileNameWithoutExtension(filepath);
            Dictionary<string, string> localVariables = new Dictionary<string, string>();

            // Check filetype
            if (Path.GetExtension(filepath).ToLower() != ".afk") {
                Console.WriteLine($"The file {filename} is not a valid AFK Script file...");
                return;
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

                        // Make this more efficient
                        while (DateTime.Now.CompareTo(date) < 0) Thread.Sleep(500);
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
                                SetVariable(variable, value, localVariables);
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

                            try
                            {
                                if (program.Trim() == string.Empty) Process.Start(arguments.TrimEnd());
                                else Process.Start(program.Trim(), arguments.TrimEnd());
                            }
                            catch (System.ComponentModel.Win32Exception)
                            {
                                Console.WriteLine($"Failed to start program: \"{program.Trim()}\", with arguments: {arguments.TrimEnd()} on line {i}.");
                            }
                        }
                        else
                        {
                            Error_TooFewParams(instrcutionName, "[program] (argument1) (argument2) ...", filename);
                            break;
                        }
                        break;
                    case "PAUSE":
                        Console.WriteLine("Paused. Press any key to continue.");
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
					case "SET":
						if (instructionArgsCount < 2) Error_TooFewParams(instrcutionName, "[variable] [value]", filename);
						if (instructionArgsCount == 2)
						{
							string variable = instructionArgsRaw(0);
							if (variable.StartsWith("$"))
							{
								string value = instructionArgs(1);
								if (!localVariables.ContainsKey(variable)) localVariables.Add(variable, value);
								else localVariables[variable] = value;
							}
							else Error_UnexpectedToken(variable, "variable name", filename);
						}
						else Error_TooManyParams(instrcutionName, "[variable] [value]", filename);
                        break;
                    case "CLICK":
                        if (instructionArgsCount == 2)
                        {
                            int x, y;
                            if (int.TryParse(instructionArgs(0), out x) && int.TryParse(instructionArgs(1), out y))
                            {
                                Cursor.Position = new System.Drawing.Point(x, y);
                                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                            }
                            else
                            {
                                Error_UnexpectedToken(instructionArgs(0) + " or " + instructionArgs(1), "[x] [y]", filename);
                            }
                        }
                        else if (instructionArgsCount > 2)
                        {
                            Error_TooManyParams(instrcutionName, "[x] [y]", filename);
                            break;
                        }
                        else if (instructionArgsCount < 2)
                        {
                            Error_TooFewParams(instrcutionName, "[x] [y]", filename);
                            break;
                        }
                        break;
                    case "KEY":
                        if (instructionArgsCount > 0)
                        {
                            string toSend = "";
                            for (int j = 0; j < instructionArgsCount; j++)
                            {
                                toSend += $"{instructionArgs(j)} ";
                            }
                            SendKeys.Send(toSend.TrimEnd());
                        }
                        else
                        {
                            Error_TooFewParams(instrcutionName, "[key] (key) (key) ...", filename);
                            break;
                        }
                        break;
                    default:
                        Error_UnknownInstruction(instrcutionName, i, filename);
                        break;
                }
            }
        }

        private static void SetVariable(string variable, string value, Dictionary<string, string> localVariables)
        {
            if (!localVariables.ContainsKey(variable)) localVariables.Add(variable, value);
            else localVariables[variable] = value;
        }

        static Dictionary<string, string> EnvironmentVariables = new Dictionary<string, string>()
        {
            {"$TIME", DateTime.Now.ToString("HH:mm:ss")},
            {"$DATE", DateTime.Now.ToString("yyyy'/'MM'/'dd")},
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

        #region Low-Level functions

        [DllImport("user32.dll",CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        #endregion
    }
}
