using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace INIPWN_GUI
{
    // === Core INI functionality ===
    class INIModel
    {
        // Dictionary: section name → (key,value) pairs.
        public Dictionary<string, Dictionary<string, string>> Data { get; private set; }
        public string CurrentFilePath { get; set; }

        public INIModel()
        {
            Data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        public void LoadFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new Exception("File does not exist: " + filePath);

            Data.Clear();
            CurrentFilePath = filePath;
            string[] lines = File.ReadAllLines(filePath);
            string currentSection = "global";
            Data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    if (!Data.ContainsKey(currentSection))
                        Data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else if (line.Contains("="))
                {
                    int idx = line.IndexOf('=');
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
                    Data[currentSection][key] = value;
                }
            }
        }

        public void SaveFile(string filePath)
        {
            List<string> lines = new List<string>();

            // Global section (if exists)
            if (Data.ContainsKey("global") && Data["global"].Count > 0)
            {
                foreach (var kvp in Data["global"])
                    lines.Add($"{kvp.Key} = {kvp.Value}");
                lines.Add("");
            }
            // Other sections
            foreach (var section in Data.Where(s => !s.Key.Equals("global", StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add("[" + section.Key + "]");
                foreach (var kvp in section.Value)
                    lines.Add($"{kvp.Key} = {kvp.Value}");
                lines.Add("");
            }
            File.WriteAllLines(filePath, lines);
        }

        // Editing methods:
        public void SetValue(string section, string key, string value)
        {
            if (!Data.ContainsKey(section))
                throw new Exception($"Section '{section}' not found.");
            Data[section][key] = value;
        }
        public void AddSection(string section)
        {
            if (Data.ContainsKey(section))
                throw new Exception($"Section '{section}' already exists.");
            Data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        public void AddKey(string section, string key, string value)
        {
            if (!Data.ContainsKey(section))
                throw new Exception($"Section '{section}' not found.");
            if (Data[section].ContainsKey(key))
                throw new Exception($"Key '{key}' already exists in section '{section}'.");
            Data[section][key] = value;
        }
        public void RemoveKey(string section, string key)
        {
            if (!Data.ContainsKey(section))
                throw new Exception($"Section '{section}' not found.");
            if (!Data[section].ContainsKey(key))
                throw new Exception($"Key '{key}' not found in section '{section}'.");
            Data[section].Remove(key);
        }
        public void RemoveSection(string section)
        {
            if (!Data.ContainsKey(section))
                throw new Exception($"Section '{section}' not found.");
            Data.Remove(section);
        }

        // Returns a list of lines to display in the editor view.
        public List<string> GetDisplayLines()
        {
            List<string> lines = new List<string>();
            foreach (var section in Data)
            {
                lines.Add("[" + section.Key + "]");
                foreach (var kvp in section.Value)
                    lines.Add("  " + kvp.Key + " = " + kvp.Value);
                lines.Add("");
            }
            return lines;
        }
    }

    // === A simple helper for colored output ===
    static class UIHelper
    {
        /// <summary>
        /// Draws one line with syntax highlighting.
        /// If isSelected is true, the entire line is drawn with a highlighted background.
        /// </summary>
        public static void DrawColoredLine(string line, bool isSelected)
        {
            if (isSelected)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                // Section header.
                Console.ForegroundColor = isSelected ? ConsoleColor.White : ConsoleColor.Yellow;
                Console.WriteLine(line);
            }
            else if (line.Contains(" = "))
            {
                int idx = line.IndexOf(" = ");
                string key = line.Substring(0, idx);
                string value = line.Substring(idx + 3);

                Console.ForegroundColor = isSelected ? ConsoleColor.White : ConsoleColor.Green;
                Console.Write(key);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" = ");
                Console.ForegroundColor = isSelected ? ConsoleColor.White : ConsoleColor.Cyan;
                Console.WriteLine(value);
            }
            else
            {
                Console.WriteLine(line);
            }
            Console.ResetColor();
        }
    }

    // === View framework ===
    interface IView
    {
        /// <summary>
        /// Draws the view.
        /// </summary>
        void Draw();

        /// <summary>
        /// Processes input and returns an optional new view (or null to remain).
        /// </summary>
        IView ProcessInput(string input);
    }

    // === File Selection View ===
    class FileSelectionView : IView
    {
        private List<string> files;
        private int selectedIndex = 0;
        private bool showBanner;

        public FileSelectionView(bool showBanner)
        {
            this.showBanner = showBanner;
            files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.ini")
                             .Select(Path.GetFileName)
                             .ToList();
        }

        public void Draw()
        {
            Console.Clear();
            if (showBanner)
            {
                PrintBanner();
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("File Selection - Use 'w' (up), 's' (down) and press Enter to select a file. (Q to quit)");
            Console.ResetColor();
            Console.WriteLine();

            if (files.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No INI files found in the current directory.");
                Console.ResetColor();
                return;
            }
            for (int i = 0; i < files.Count; i++)
            {
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" > " + files[i]);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("   " + files[i]);
                }
            }
        }

        public IView ProcessInput(string input)
        {
            input = input.Trim().ToLower();
            if (input == "w")
                selectedIndex = Math.Max(0, selectedIndex - 1);
            else if (input == "s")
                selectedIndex = Math.Min(files.Count - 1, selectedIndex + 1);
            else if (input == "q")
                Environment.Exit(0);
            else if (input == "" || input == "enter")
            {
                string fileName = files[selectedIndex];
                INIModel model = new INIModel();
                try
                {
                    model.LoadFile(fileName);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error loading file: " + ex.Message);
                    Console.ResetColor();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                    return this;
                }
                return new EditorView(model);
            }
            return null;
        }

        void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.DarkRed; // banner art made by Máté, ty <3
            Console.WriteLine("░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
            Console.WriteLine("░██╗███╗░░░██╗██╗██████╗░██╗░░░░██╗███╗░░░██╗░");
            Console.WriteLine("░██║████╗░░██║██║██╔══██╗██║░░░░██║████╗░░██║░");
            Console.WriteLine("░██║██╔██╗░██║██║██████╔╝██║░█╗░██║██╔██╗░██║░");
            Console.WriteLine("░██║██║╚██╗██║██║██╔═══╝░██║███╗██║██║╚██╗██║░");
            Console.WriteLine("░██║██║░╚████║██║██║░░░░░╚███╔███╔╝██║░╚████║░");
            Console.WriteLine("░╚═╝╚═╝░░╚═══╝╚═╝╚═╝░░░░░░╚══╝╚══╝░╚═╝░░╚═══╝░");
            Console.WriteLine("░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
            Console.WriteLine("          Enhanced INI File Manipulator v3.0");
            Console.ResetColor();
        }
    }

    // === Editor View (normal command–based view) ===
    class EditorView : IView
    {
        protected INIModel model;
        protected List<string> displayLines;
        protected int scrollOffset = 0;
        protected int contentHeight;

        // Search state: if a search is active, these store matching line indices.
        protected List<int> searchIndices = new List<int>();
        protected int currentSearchPointer = 0;
        protected string lastSearchTerm = "";

        public EditorView(INIModel model)
        {
            this.model = model;
            UpdateDisplayLines();
            contentHeight = Console.WindowHeight - 4; // Reserve header and prompt lines.
        }

        public virtual void Draw()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Editor - File: " + (string.IsNullOrEmpty(model.CurrentFilePath) ? "None" : model.CurrentFilePath) + $"| Ln: {scrollOffset+1}-{Math.Min(displayLines.Count, scrollOffset+contentHeight)}/{displayLines.Count}");
            Console.ResetColor();
            Console.WriteLine(new string('-', Console.WindowWidth));

            // Clamp scrollOffset
            if (scrollOffset < 0)
                scrollOffset = 0;
            if (scrollOffset > Math.Max(0, displayLines.Count - contentHeight))
                scrollOffset = Math.Max(0, displayLines.Count - contentHeight);

            // Display visible lines with syntax highlighting.
            for (int i = 0; i < contentHeight; i++)
            {
                int lineIndex = scrollOffset + i;
                if (lineIndex < displayLines.Count)
                    UIHelper.DrawColoredLine(displayLines[lineIndex], false);
                else
                    Console.WriteLine();
            }
            Console.WriteLine(new string('-', Console.WindowWidth));
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("> ");
            Console.ResetColor();
        }

        public virtual IView ProcessInput(string input)
        {
            input = input.Trim();
            // If a search is active, allow iteration with 'n' and 'p'
            if (input.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                if (searchIndices.Count > 0)
                {
                    currentSearchPointer = (currentSearchPointer + 1) % searchIndices.Count;
                    scrollOffset = searchIndices[currentSearchPointer];
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Next match ({currentSearchPointer + 1}/{searchIndices.Count}).");
                    Console.ResetColor();
                    Pause();
                }
                return null;
            }
            else if (input.Equals("p", StringComparison.OrdinalIgnoreCase))
            {
                if (searchIndices.Count > 0)
                {
                    currentSearchPointer = (currentSearchPointer - 1 + searchIndices.Count) % searchIndices.Count;
                    scrollOffset = searchIndices[currentSearchPointer];
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Previous match ({currentSearchPointer + 1}/{searchIndices.Count}).");
                    Console.ResetColor();
                    Pause();
                }
                return null;
            }
            // Clear search state if another command is entered.
            if (!input.StartsWith("search", StringComparison.OrdinalIgnoreCase))
            {
                searchIndices.Clear();
                lastSearchTerm = "";
            }

            if (input.Equals("w", StringComparison.OrdinalIgnoreCase))
            {
                scrollOffset = Math.Max(0, scrollOffset - 1);
            }
            else if (input.Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                scrollOffset = Math.Min(displayLines.Count - contentHeight, scrollOffset + 1);
            }
            else if (input.Equals("back", StringComparison.OrdinalIgnoreCase))
            {
                return new FileSelectionView(false);
            }
            else if (input.StartsWith("search ", StringComparison.OrdinalIgnoreCase))
            {
                string term = input.Substring(7).Trim().ToLower();
                if (string.IsNullOrEmpty(term))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please provide a term to search for.");
                    Console.ResetColor();
                    Pause();
                }
                else
                {
                    lastSearchTerm = term;
                    searchIndices = new List<int>();
                    for (int i = 0; i < displayLines.Count; i++)
                    {
                        if (displayLines[i].ToLower().Contains(term))
                            searchIndices.Add(i);
                    }
                    if (searchIndices.Count > 0)
                    {
                        currentSearchPointer = 0;
                        scrollOffset = searchIndices[currentSearchPointer];
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{searchIndices.Count} match(es) found. Use 'n' for next and 'p' for previous.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("No matches found.");
                        Console.ResetColor();
                    }
                    Pause();
                }
            }
            else if (input.Equals("edit", StringComparison.OrdinalIgnoreCase))
            {
                return new InlineEditorView(model);
            }
            else if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(0);
            }
            else if (input.Length > 0)
            {
                List<string> args = ParseArgs(input);
                if (args.Count == 0)
                    return null;
                string command = args[0].ToLower();
                try
                {
                    switch (command)
                    {
                        case "help":
                            ShowHelp();
                            Pause();
                            break;
                        case "save":
                            if (args.Count >= 2)
                                model.CurrentFilePath = args[1];
                            if (string.IsNullOrEmpty(model.CurrentFilePath))
                                throw new Exception("No file specified for saving.");
                            model.SaveFile(model.CurrentFilePath);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Saved INI file to " + model.CurrentFilePath);
                            Console.ResetColor();
                            Pause();
                            break;
                        case "set":
                            if (args.Count < 4)
                                throw new Exception("Usage: set <section> <key> <value>");
                            string value = string.Join(" ", args.Skip(3));
                            model.SetValue(args[1], args[2], value);
                            break;
                        case "goto":
                            if (int.TryParse(args[1], out int lineNum) && lineNum > 0 && lineNum <= displayLines.Count)
                            {
                                scrollOffset = lineNum - 1;
                            }
                            break;
                        case "addsection":
                            if (args.Count < 2)
                                throw new Exception("Usage: addsection <section>");
                            model.AddSection(args[1]);
                            break;
                        case "addkey":
                            if (args.Count < 4)
                                throw new Exception("Usage: addkey <section> <key> <value>");
                            value = string.Join(" ", args.Skip(3));
                            model.AddKey(args[1], args[2], value);
                            break;
                        case "removekey":
                            if (args.Count < 3)
                                throw new Exception("Usage: removekey <section> <key>");
                            model.RemoveKey(args[1], args[2]);
                            break;
                        case "removesection":
                            if (args.Count < 2)
                                throw new Exception("Usage: removesection <section>");
                            model.RemoveSection(args[1]);
                            break;
                        case "list":
                            // Refresh display.
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Unknown command. Type 'help' for a list of commands.");
                            Console.ResetColor();
                            Pause();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: " + ex.Message);
                    Console.ResetColor();
                    Pause();
                }
                UpdateDisplayLines();
            }
            return null;
        }

        protected void UpdateDisplayLines()
        {
            displayLines = model.GetDisplayLines();
        }

        protected void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Editor Help:");
            Console.ResetColor();
            Console.WriteLine("  help                            - Show this help text.");
            Console.WriteLine("  save [\"<filePath>\"]           - Save the INI file.");
            Console.WriteLine("  set <section> <key> <value>     - Set or update a key’s value.");
            Console.WriteLine("  addsection <section>            - Add a new section.");
            Console.WriteLine("  addkey <section> <key> <value>  - Add a new key/value pair.");
            Console.WriteLine("  removekey <section> <key>       - Remove a key.");
            Console.WriteLine("  removesection <section>         - Remove a section.");
            Console.WriteLine("  search <term>                   - Jump to a search match.");
            Console.WriteLine("     After searching, use 'n' for next, 'p' for previous.");
            Console.WriteLine("  edit                            - Switch to inline editing mode.");
            Console.WriteLine("  goto                            - Go to a specific line.");
            Console.WriteLine("  list                            - Refresh display.");
            Console.WriteLine("  back                            - Return to file selection view.");
            Console.WriteLine("  q                               - Quit the program.");
            Console.WriteLine("");
        }

        protected List<string> ParseArgs(string input)
        {
            List<string> args = new List<string>();
            bool inQuotes = false;
            string current = "";
            foreach (char ch in input)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    if (!string.IsNullOrEmpty(current))
                    {
                        args.Add(current);
                        current = "";
                    }
                }
                else
                {
                    current += ch;
                }
            }
            if (!string.IsNullOrEmpty(current))
                args.Add(current);
            return args;
        }

        protected void Pause()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }

    // === Inline Editor View (allows in–place editing of key values) ===
    class InlineEditorView : IView
    {
        private INIModel model;
        private List<DetailedLine> detailedLines;
        private int scrollOffset = 0;
        private int contentHeight;
        private int selectedIndex = 0;

        private class DetailedLine
        {
            public string Text;
            public bool IsSection;
            public bool IsKeyValue;
            public string Section;
            public string Key;
        }

        public InlineEditorView(INIModel model)
        {
            this.model = model;
            BuildDetailedLines();
            contentHeight = Console.WindowHeight - 4;
        }

        private void BuildDetailedLines()
        {
            detailedLines = new List<DetailedLine>();
            foreach (var section in model.Data)
            {
                DetailedLine secLine = new DetailedLine
                {
                    Text = "[" + section.Key + "]",
                    IsSection = true,
                    IsKeyValue = false
                };
                detailedLines.Add(secLine);
                foreach (var kvp in section.Value)
                {
                    DetailedLine kvLine = new DetailedLine
                    {
                        Text = "  " + kvp.Key + " = " + kvp.Value,
                        IsSection = false,
                        IsKeyValue = true,
                        Section = section.Key,
                        Key = kvp.Key
                    };
                    detailedLines.Add(kvLine);
                }
                detailedLines.Add(new DetailedLine { Text = "", IsSection = false, IsKeyValue = false });
            }
        }

        public void Draw()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Inline Editor - File: " + (string.IsNullOrEmpty(model.CurrentFilePath) ? "None" : model.CurrentFilePath) + $" | Ln: {selectedIndex+1}/{detailedLines.Count}");
            Console.ResetColor();
            Console.WriteLine(new string('-', Console.WindowWidth));

            if (selectedIndex < 0) selectedIndex = 0;
            if (selectedIndex >= detailedLines.Count) selectedIndex = detailedLines.Count - 1;
            if (scrollOffset > selectedIndex)
                scrollOffset = selectedIndex;
            if (selectedIndex >= scrollOffset + contentHeight)
                scrollOffset = selectedIndex - contentHeight + 1;

            for (int i = 0; i < contentHeight; i++)
            {
                int lineIndex = scrollOffset + i;
                if (lineIndex < detailedLines.Count)
                {
                    bool isSelected = (lineIndex == selectedIndex);
                    UIHelper.DrawColoredLine(detailedLines[lineIndex].Text, isSelected);
                }
                else
                    Console.WriteLine();
            }
            Console.WriteLine(new string('-', Console.WindowWidth));
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Inline Editor > ");
            Console.ResetColor();
        }

        public IView ProcessInput(string input)
        {
            input = input.Trim();
            if (input.Equals("w", StringComparison.OrdinalIgnoreCase))
                selectedIndex = Math.Max(0, selectedIndex - 1);
            else if (input.Equals("s", StringComparison.OrdinalIgnoreCase))
                selectedIndex = Math.Min(detailedLines.Count - 1, selectedIndex + 1);
            else if (input.Equals("back", StringComparison.OrdinalIgnoreCase))
                return new EditorView(model);
            else if (input.Equals("e", StringComparison.OrdinalIgnoreCase))
            {
                DetailedLine line = detailedLines[selectedIndex];
                if (line.IsKeyValue)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"Editing [{line.Section}] {line.Key}. Enter new value: ");
                    Console.ResetColor();
                    string newValue = Console.ReadLine();
                    try
                    {
                        model.SetValue(line.Section, line.Key, newValue);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Value updated.");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: " + ex.Message);
                        Console.ResetColor();
                    }
                    Pause();
                    BuildDetailedLines();
                }
            }
            else if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(0);
            }
            else if (input.StartsWith("goto "))
            {
                if (int.TryParse(input.Substring(5), out int lineNum) && lineNum > 0 && lineNum <= detailedLines.Count)
                {
                    scrollOffset = lineNum - 1;
                }
            }
            return null;
        }

        private void Pause()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }

    // === Main Program ===
    class Program
    {
        static void Main(string[] args)
        {
            // Version flag.
            if (args.Length > 0 && (args[0] == "-v" || args[0] == "--version"))
            {
                PrintVersionBanner();
                return;
            }

            IView currentView = null;
            if (args.Length > 0 && File.Exists(args[0]))
            {
                INIModel model = new INIModel();
                try
                {
                    model.LoadFile(args[0]);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error loading file: " + ex.Message);
                    Console.ResetColor();
                    return;
                }
                currentView = new EditorView(model);
            }
            else
            {
                currentView = new FileSelectionView(true);
            }

            while (true)
            {
                currentView.Draw();
                string input = Console.ReadLine();
                IView newView = currentView.ProcessInput(input);
                if (newView != null)
                    currentView = newView;
            }
        }

        static void PrintVersionBanner()
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
            Console.WriteLine("░██╗███╗░░░██╗██╗██████╗░██╗░░░░██╗███╗░░░██╗░");
            Console.WriteLine("░██║████╗░░██║██║██╔══██╗██║░░░░██║████╗░░██║░");
            Console.WriteLine("░██║██╔██╗░██║██║██████╔╝██║░█╗░██║██╔██╗░██║░");
            Console.WriteLine("░██║██║╚██╗██║██║██╔═══╝░██║███╗██║██║╚██╗██║░");
            Console.WriteLine("░██║██║░╚████║██║██║░░░░░╚███╔███╔╝██║░╚████║░");
            Console.WriteLine("░╚═╝╚═╝░░╚═══╝╚═╝╚═╝░░░░░░╚══╝╚══╝░╚═╝░░╚═══╝░");
            Console.WriteLine("░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
            Console.WriteLine("          Enhanced INI File Manipulator v3.0");
            Console.ResetColor();
        }
    }
}
