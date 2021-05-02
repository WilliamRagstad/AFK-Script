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
using NLua;

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
            args = new[] {@"Examples\lua.afk"};

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
            Console.ReadKey(true);
        }
        
        static Dictionary<string, Variable> EnvironmentVariables = new Dictionary<string, Variable>()
        {
            {"$TIME", Variable.Create("TIME", DataType.DateTime, _ => DateTime.Now.ToString("HH:mm:ss"))},
            {"$DATE", Variable.Create("DATE", DataType.DateTime, DateTime.Now.ToString("yyyy'/'MM'/'dd"))},
            {"$ACTIVE_PROGRAM", Variable.Create("ACTIVE_PROGRAM", DataType.String, _ => GetActiveWindowTitle() ?? "")},

            {"$TEST", Variable.Create("TEST", DataType.String, "Hello, this is a general test :D")}
        };

        static bool RunFailed;
        static void Run(object parameters)
        {
            string filepath = parameters.ToString();
            string filename = Path.GetFileName(filepath);
            string rawfilename = Path.GetFileNameWithoutExtension(filepath);
            Dictionary<string, Variable> localVariables = EnvironmentVariables; // Start with the environment variables

            // Check filetype
            if (Path.GetExtension(filepath).ToLower() != ".afk") {
                Console.WriteLine($"The file {filename} is not a valid AFK Script file...");
                return;
            }

            string[] instructions = File.ReadAllLines(filepath);
            Dictionary<string, int> labels = LocateLabels(instructions);

            bool strict = false;
            RunFailed = false;
            for (int i = 0; i < instructions.Length; i++)
            {
                if (strict && RunFailed) return;
                string[] instructionParts = Regex.Matches(instructions[i].Split('#')[0], "(\"[^\"\\n]+\")|([^\\x20\\t\\n]+)", RegexOptions.Compiled).Cast<Match>().Select(
                    p => p.Value
                ).ToArray();

                string instrcutionName = instructionParts.Length > 0 ? instructionParts[0].ToUpper() : "";
                int instructionArgsCount = instructionParts.Length - 1;
                string instructionArg(int index) {
                    string arg = SubstituteVariables(instructionParts[index + 1].Trim(), localVariables).TrimStart('"').TrimEnd('"');
                    if (ContainsVariable(arg))
                    {
                        Console.WriteLine($"Unknown variable found at line {i+1} in '{arg}'.");
                        RunFailed = true;
                    }
                    return arg;
                }
                string instructionArgRaw(int index) => instructionParts[index + 1].Trim();

                string[] instructionArgsBetween(int from, int to) {
                    List<string> result = new List<string>();
                    if (to > instructionArgsCount) throw new ArgumentException("Outside bounds", nameof(to));
                    for (int j = from; j < to; j++) result.Add(instructionArgRaw(j));
                    return result.ToArray();
                }
                
                #region Instructions
                if (instrcutionName == "") continue;
                switch (instrcutionName)
                {
                    case "AT":

                        DateTime date = DateTime.Now;
                        if (instructionArgsCount == 1)      // DATE or TIME
                        {
                            date = DateTime.Parse(instructionArg(0),
                                System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else if (instructionArgsCount == 2) // DATE and TIME
                        {
                            date = DateTime.Parse(instructionArg(0) + " " + instructionArg(1),
                                System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Error_TooManyParams(instrcutionName, "DATE or TIME or (DATE and TIME)", filename);
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
                            string text = instructionArg(0);
                            if (childProcesses) Console.WriteLine(rawfilename + ": " + text);
                            else Console.WriteLine(text);
                        }
                        else Error_TooManyParams(instrcutionName, 1, filename);
                        break;
                    case "READ":
                        if (instructionArgsCount == 1)
                        {
                            string variable = instructionArgRaw(0);
                            if (variable.StartsWith("$"))
                            {
                                string value = Console.ReadLine();
                                SetVariable(variable, DataType.String, value, localVariables);
                            }
                            else Error_UnexpectedToken(variable, "variable name", filename);
                        }
                        else if (instructionArgsCount == 2)
                        {
                            string variable = instructionArgRaw(0);
                            if (variable.StartsWith("$"))
                            {
                                string text = instructionArg(1);
                                Console.Write(text);
                                string value = Console.ReadLine();
                                SetVariable(variable, DataType.String, value, localVariables);
                            }
                            else Error_UnexpectedToken(variable, "variable name", filename);
                        }
                        else if (instructionArgsCount == 0) Error_TooFewParams(instrcutionName, "[text] [variable] or [variable]", filename);
                        else Error_TooManyParams(instrcutionName, "[text] [variable] or [variable]", filename);
                        break;
                    case "START":
                        if (instructionArgsCount > 0)
                        {
                            string program = instructionArg(0);
                            string arguments = "";

                            for (int j = 1; j < instructionArgsCount; j++) arguments += $"\"{instructionArg(j)}\" ";

                            try
                            {
                                if (program.Trim() == string.Empty) Process.Start(arguments.TrimEnd());
                                else Process.Start(program.Trim(), arguments.TrimEnd());
                            }
                            catch (System.ComponentModel.Win32Exception)
                            {
                                Console.WriteLine($"Failed to start program: \"{program.Trim()}\", with arguments: {arguments.TrimEnd()} on line {i+1}.");
                                RunFailed = true;
                            }
                        }
                        else Error_TooFewParams(instrcutionName, "[program] (argument1) (argument2) ...", filename);
                        break;
                    case "PAUSE":
                        Console.WriteLine("Paused. Press any key to continue.");
                        Console.ReadKey(true);
                        break;
                    case "WAIT":
                        if (instructionArgsCount == 1)
                        {
                            string durationRaw = instructionArg(0);
                            if (int.TryParse(durationRaw, out int duration)) Thread.Sleep(duration);
                            else Error_UnexpectedToken(durationRaw, "[milliseconds]", filename);
                        }
                        else Error_TooManyParams(instrcutionName, "[milliseconds]", filename);
                        break;
					case "SET":
						if (instructionArgsCount < 2) Error_TooFewParams(instrcutionName, "[variable] [value]", filename);
						if (instructionArgsCount == 2)
						{
							string variable = instructionArgRaw(0);
							if (variable.StartsWith("$"))
							{
								string value = instructionArg(1);
                                DataType type = getValueType(value, i);
                                SetVariable(variable, type, value, localVariables);
							}
							else Error_UnexpectedToken(variable, "variable name", filename);
						}
						else
                        {
                            // Lua expression
                            string variable = instructionArgRaw(0);
							if (variable.StartsWith("$"))
							{
								string valueRaw = string.Join(" ", instructionArgsBetween(1, instructionArgsCount));
                                if (Evaluate(valueRaw, i, localVariables, false, out object value)) SetVariable(variable, DataType.String, value, localVariables);
                                else {
                                    Console.WriteLine($"Could not evaluate codition on line {i+1}: '{valueRaw}'");
                                    RunFailed = true;
                                }
							}
							else Error_UnexpectedToken(variable, "variable name", filename);
                        }
                        break;
                    case "GOTO":
                        if (instructionArgsCount == 1)
                        {
							string glabel = instructionArg(0).ToUpper();
                            if (labels.ContainsKey(glabel)) i = labels[glabel]; // Goto the first instruction after the label, i is incremented after break.
                            else {
                                Console.WriteLine($"Unknown label '{glabel}'! Goto skipped");
                                RunFailed = true;
                            }
                        }
                        else if (instructionArgsCount > 2) Error_TooManyParams(instrcutionName, "[label]", filename);
                        else if (instructionArgsCount < 2) Error_TooFewParams(instrcutionName, "[label]", filename);
                        break;
                    case "CLICK":
                        if (instructionArgsCount == 0)
                        {
                            // Console.WriteLine($"Clicked at {Cursor.Position.X}, {Cursor.Position.Y}");
                            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                        }
                        else if (instructionArgsCount == 2)
                        {
                            int x, y;
                            if (int.TryParse(instructionArg(0), out x) && int.TryParse(instructionArg(1), out y))
                            {
                                Cursor.Position = new System.Drawing.Point(x, y);
                                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                            }
                            else Error_UnexpectedToken(instructionArg(0) + " or " + instructionArg(1), "[x] [y]", filename);
                        }
                        else if (instructionArgsCount > 2) Error_TooManyParams(instrcutionName, "[x] [y]", filename);
                        else if (instructionArgsCount < 2) Error_TooFewParams(instrcutionName, "[x] [y]", filename);
                        break;
                    case "KEY":
                        if (instructionArgsCount > 0)
                        {
                            string toSend = "";
                            for (int j = 0; j < instructionArgsCount; j++)
                            {
                                toSend += $"{instructionArg(j)} ";
                            }
                            SendKeys.Send(toSend.TrimEnd());
                        }
                        else Error_TooFewParams(instrcutionName, "[key] (key) (key) ...", filename);
                        break;
                    case "IF":
                        string condition = string.Join(" ", instructionArgsBetween(0, instructionArgsCount - 1));
                        string ilabel = instructionArgRaw(instructionArgsCount - 1).ToUpper();
                        if (Evaluate(condition, i, localVariables, false, out bool willJump))
                        {
                            if (willJump)
                            {
                                if (labels.ContainsKey(ilabel)) i = labels[ilabel];
                                else {
                                    Console.WriteLine($"Could not find label '{ilabel}' on line {i+1}");
                                    RunFailed = true;
                                }
                            }
                        }
                        else {
                            Console.WriteLine($"Could not evaluate codition on line {i+1}: '{condition}'");
                            RunFailed = true;
                        }
                        break;
                    default:
                        if (instrcutionName.StartsWith(":")) continue; // Labels are not instructions and are located in the first pass
                        if (instrcutionName.StartsWith("@"))
                        {
                            switch(instrcutionName.TrimStart('@'))
                            {
                                case "STRICT":
                                    strict = true;
                                    break;
                                default:
                                    Error_UnknownFlag(instrcutionName, i, filename);
                                    RunFailed = true;
                                    break;
                            }
                        }
                        else {
                            Error_UnknownInstruction(instrcutionName, i, filename);
                            RunFailed = true;
                        }
                        break;
                }
                #endregion
            }
        }

        #region Type System
        private static DataType getValueType(string value, int line)
        {
            if (int.TryParse(value, out int _)) return DataType.Number;
            if (DateTime.TryParse(value, out DateTime _)) return DataType.DateTime;
            if (new[] { "TRUE", "FALSE" }.Contains(value.ToUpper())) return DataType.Boolean;
            return DataType.String;
        }
        #endregion

        #region Expression Evaluation
        private static bool Evaluate<T>(string expression, int line, Dictionary<string, Variable> variables, bool failIfNotConvertable, out T result)
        {
            expression = ReplaceAll(expression, "$", ""); // Remove all $ from variable names
            expression = ReplaceAll(expression, "!=", "~="); // Change all != into Lua not equals ~=
            using (Lua state = new Lua())
            {
                foreach (var variable in variables) state[variable.Value.Name] = variable.Value.Value.Get();

                try
                {
                    object[] evaluatedResult = state.DoString("return " + expression);
                    if (evaluatedResult.Length > 0)
                    {
                        object value = evaluatedResult[0];
                        try
                        {
                            result = (T)Convert.ChangeType(value, typeof(T)); // IConvertible
                            return true;
                        }
                        catch (Exception)
                        {
                            if (!failIfNotConvertable) {
                                result = default(T);
                                return true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            
            RunFailed = true;
            result = default(T);
            return false;
        }

        static string ReplaceAll(string source, string find, string replace)
        {
            while(source.Contains(find)) source = source.Replace(find, replace);
            return source;
        }
        #endregion

        #region Label Functions
        private static Dictionary<string, int> LocateLabels(string[] instructions)
        {
            Dictionary<string, int> labels = new Dictionary<string, int>();
            for (int i = 0; i < instructions.Length; i++)
            {
                if (instructions[i].Trim().StartsWith(":"))
                {
                    string label = instructions[i].Trim().Replace(":","").ToUpper();
                    if (!labels.ContainsKey(label)) labels.Add(label, i); // Found label
                    else Console.WriteLine($"Warning, duplicate labels found on line {labels[label]+1} and line {i+1}! Using first occurence of label.");
                }
            }
            return labels;
        }
        #endregion

        #region Variable Handling
        private static void SetVariable(string variable, DataType type, dynamic value, Dictionary<string, Variable> localVariables)
        {
            if (!localVariables.ContainsKey(variable)) localVariables.Add(variable, Variable.Create(variable.TrimStart('$'), type, value));
            else localVariables[variable] = Variable.Create(variable.TrimStart('$'), type, value);
        }

        static bool ContainsVariable(string input)
        {
            if (input.Contains('$') && input.Length > 1) return true;
            return false;
        }

        static string SubstituteVariables(string input, Dictionary<string, Variable> variables)
        {
            if (!ContainsVariable(input)) return input;
            
            for (int i = LongestVariableName(variables); i > 0; i--)
            {
                Dictionary<string, Variable> vars = VariablesWithNameLengthOf(i, variables);
                if (vars.Count == 0) continue;
                foreach (KeyValuePair<string, Variable> var in vars)
                {
                    while (input.Contains(var.Key)) input = input.Replace(var.Key, var.Value.Value.Get()?.ToString());
                }
            }

            return input;
        }

        static int LongestVariableName(Dictionary<string, Variable> variables)
        {
            int longest = 0;
            foreach (KeyValuePair<string, Variable> var in variables)
            {
                int len = var.Key.Length;
                if (len > longest) longest = len;
            }
            return longest;
        }

        static Dictionary<string, Variable> VariablesWithNameLengthOf(int length, Dictionary<string, Variable> variables)
        {
            Dictionary<string, Variable> result = new Dictionary<string, Variable>();
            foreach (KeyValuePair<string, Variable> var in variables)
            {
                if (var.Key.Length == length) result.Add(var.Key, var.Value);
            }
            return result;
        }
        #endregion

        #region Error Messages
        static void Error_Unknown(string kind, string what, int line, string file)
        {
            if (childProcesses) Console.Write($"Error in {file}: ");
            Console.WriteLine($"Unknown {kind} '{what}' at line {line + 1}!");
            RunFailed = true;
        }
        static void Error_UnknownInstruction(string instruction, int line, string file) => Error_Unknown("instruction", instruction, line, file);
        static void Error_UnknownFlag(string flag, int line, string file)  => Error_Unknown("flag", flag, line, file);
        static void Error_TooManyParams(string instruction, object expected, string file)
        {
            if (childProcesses) Console.Write($"Error in {file}: ");
            Console.WriteLine($"Too many parameters for instruction {instruction}! Expected {expected}...");
            RunFailed = true;
        }
        static void Error_TooFewParams(string instruction, object expected, string file)
        {
            if (childProcesses) Console.Write($"Error in {file}: ");
            Console.WriteLine($"Too few parameters for instruction {instruction}! Expected {expected}...");
            RunFailed = true;
        }
        static void Error_UnexpectedToken(string token, object expected, string file)
        {
            if (childProcesses) Console.Write($"Error in {file}: ");
            Console.WriteLine($"Unexpected token {token}! Expected {expected}...");
            RunFailed = true;
        }
        #endregion

        #region Low-Level functions

        [DllImport("user32.dll",CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        #endregion
    }
}
