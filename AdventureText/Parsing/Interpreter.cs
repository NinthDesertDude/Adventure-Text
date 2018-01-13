using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace AdventureText.Parsing
{
    /// <summary>
    /// Handles entries provided by the parser and pushes changes to a
    /// console.
    /// </summary>
    class Interpreter
    {
        #region Fields
        /// <summary>
        /// Tracked so they can be removed when navigating other forks/files.
        /// </summary>
        private List<Console.OnSubmitHandler> _actions;

        /// <summary>
        /// Stores the console being used.
        /// </summary>
        private Console _console;

        /// <summary>
        /// Stores all tree entries.
        /// </summary>
        private Dictionary<string, ParseNode> _entries;

        /// <summary>
        /// Stores the current page by name.
        /// </summary>
        private string _fork;

        /// <summary>
        /// If true, in-line options are stylized to look like text.
        /// </summary>
        private bool _optLinkStyleHidden;

        /// <summary>
        /// The text to be displayed when there are no options.
        /// </summary>
        private string _optOptionDefault;

        /// <summary>
        /// Whether to generate an option when none exist. May be disabled to
        /// provide options based on input or timers.
        /// </summary>
        private bool _optOptionEnabled;

        /// <summary>
        /// Used to generate random values when invoked by user syntax.
        /// </summary>
        private Random _rng;

        /// <summary>
        /// If true, exceptions are thrown instead of silently failing.
        /// </summary>
        private static bool _showErrors = false;

        /// <summary>
        /// If true, exceptions are caught and printed to the screen.
        /// </summary>
        private static bool _printErrors = false;

        /// <summary>
        /// Tracked so they can be stopped when navigating other forks/files.
        /// </summary>
        private List<DispatcherTimer> _timers;

        /// <summary>
        /// Defines a place for generated variables to be stored and accessed.
        /// </summary>
        private Dictionary<string, object> _variables;

        /// <summary>
        /// Used to stop evaluation of the current fork entirely.
        /// </summary>
        private bool _stopEvaluation;
        #endregion

        #region Properties
        /// <summary>
        /// Stores the current page by name. Changing the fork automatically
        /// loads the page with the given name.
        /// </summary>
        public string Fork
        {
            get
            {
                return _fork;
            }
        }

        /// <summary>
        /// Stores all parsed entries as individual trees.
        /// </summary>
        public Dictionary<string, ParseNode> Entries
        {
            get
            {
                return _entries;
            }
        }

        /// <summary>
        /// If true, all errors that occur while parsing will be thrown as
        /// exceptions. Otherwise, they will silently fail.
        /// </summary>
        public static bool ShowErrors
        {
            get
            {
                return _showErrors;
            }
            set
            {
                _showErrors = value;
            }
        }

        /// <summary>
        /// If true, exceptions are caught and printed to the screen.
        /// </summary>
        public static bool PrintErrors
        {
            get
            {
                return _printErrors;
            }
            set
            {
                _printErrors = value;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new interpreter with a console to make changes to.
        /// </summary>
        /// <param name="console">
        /// The console to be updated with changes by the interpreter.
        /// </param>
        public Interpreter(Console console)
        {
            _actions = new List<Console.OnSubmitHandler>();
            _console = console;
            _entries = new Dictionary<string, ParseNode>();
            _fork = String.Empty;
            _optLinkStyleHidden = false;
            _optOptionDefault = "restart";
            _optOptionEnabled = true;
            _rng = new Random();
            _timers = new List<DispatcherTimer>();
            _variables = new Dictionary<string, object>();
            _stopEvaluation = false;

            console.OnKeyDown += OpenCommandDialog;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Opens a command input dialog to process inputted commands.
        /// </summary>
        private void OpenCommandDialog(object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            //Exits unless Ctrl + Space is being pressed.
            if ((!e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                !e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.RightCtrl)) ||
                e.Key != System.Windows.Input.Key.Space)
            {
                return;
            }

            DebugCommand debugWindow = new DebugCommand();

            //Handles code execution for the debug window.
            debugWindow.OnExecuteQuery += new Action<string>((a) =>
            {
                bool isACommand = true;

                //Attempts to process the command as normal otherwise.
                ParseNode textNode = new ParseNode();
                textNode.Text = a;

                try
                {
                    ProcessText(textNode);
                }

                //If not a valid header or normal command, it is bad.
                catch (Exception ex)
                {
                    debugWindow.SetExecInfo("Command not understood. Error: " +
                        ex.Message);

                    isACommand = false;
                }

                if (isACommand)
                {
                    debugWindow.SetExecInfo("Command executed.");
                    debugWindow.RefreshLists();
                }
            });

            //Handles populating the list of forks for the debug window.
            debugWindow.OnLoadForks += new Action(() =>
            {
                debugWindow.ClearForks();
                List<string> forkNames = Entries.Keys.ToList();

                foreach (string name in forkNames)
                {
                    Run option = new Run(name + "\n");

                    //Highlights forks under the mouse.
                    option.MouseEnter += new System.Windows.Input.MouseEventHandler((a, b) =>
                    {
                        option.Background = Brushes.AliceBlue;
                    });

                    option.MouseLeave += new System.Windows.Input.MouseEventHandler((a, b) =>
                    {
                        option.Background = Brushes.White;
                    });

                    //Navigates to a fork when clicked.
                    option.MouseDown += new System.Windows.Input.MouseButtonEventHandler((a, b) =>
                    {
                        SetFork(name);
                        debugWindow.RefreshLists();
                    });

                    debugWindow.AddToForks(option);
                }
            });

            //Handles populating the list of variables for the debug window.
            debugWindow.OnLoadVariables += new Action(() =>
            {
                debugWindow.ClearVariables();
                var varNames = _variables;

                for (int i = 0; i < varNames.Count; i++)
                {
                    string varName = varNames.Keys.ElementAt(i);
                    string varValue = varNames.Values.ElementAt(i).ToString();
                    Run option = new Run(varName + " = " + varValue + "\n");
                    debugWindow.AddToVariables(option);
                }
            });

            debugWindow.ShowDialog();
        }

        /// <summary>
        /// For internal use. Sets the entries usually given by the parser.
        /// </summary>
        public void SetEntries(Dictionary<string, ParseNode> entries)
        {
            SetEntries(entries, String.Empty);
        }

        /// <summary>
        /// For internal use. Sets the entries usually given by the parser.
        /// </summary>
        public void SetEntries(Dictionary<string, ParseNode> entries,
            string forkToLoad)
        {
            _entries = entries;
            if (_entries.Count == 0)
            {
                if (ShowErrors)
                {
                    throw new InterpreterException("Interpreter: cannot " +
                        "load file. File contains no entries. Use @ at " +
                        "the beginning of a line to denote an entry.");
                }
            }
            else
            {
                //Tries to load a passed in fork.
                if (forkToLoad != String.Empty &&
                    Entries.Keys.Contains(forkToLoad))
                {
                    SetFork(forkToLoad);
                }

                //Loads the first entry if not found or set.
                else
                {
                    SetFork(Entries.First().Key);
                }
            }
        }

        /// <summary>
        /// For internal use. Sets the fork usually given by parsed entries.
        /// </summary>
        public void SetFork(string fork)
        {
            _fork = fork;
            _stopEvaluation = false;

            if (_printErrors)
            {
                TryLoadFork();
            }
            else
            {
                LoadFork();
            }
        }

        /// <summary>
        /// Loads an entry and pushes changes to the page, catching and
        /// displaying errors on the screen.
        /// </summary>
        private void TryLoadFork()
        {
            try
            {
                LoadFork();
            }
            catch (Exception ex)
            {
                _console.Clear();
                _console.PrefOutColor = Brushes.Yellow;

                if (ex is ParserException ||
                    ex is InterpreterException)
                {
                    _console.AddText("\nA loading error occurred. " + ex.Message);
                }
                else
                {
                    _console.AddText("\nAn error occurred. " + ex.Message);
                }

                _console.PrefOutColor = Brushes.White;

                _console.AddOption("Click to load a file.", new Action(() =>
                {
                    Utils.TryLoadFile(_console, String.Empty, String.Empty);
                }));
            }
        }

        /// <summary>
        /// Loads an entry and pushes changes to the page.
        /// </summary>
        /// <exception cref="InterpreterException">
        /// Thrown for missing images and forks, syntax errors in ifs and
        /// other commands, 
        /// </exception>
        private void LoadFork()
        {
            //Clears the console and hides the textbox by default.
            _console.Clear();
            _console.SetInputEnabled(false);

            //Resets the speech engine by removing grammars and not speaking.
            _console.SpeechEngine.UnloadAll();
            _console.SpeechEngine.ListenStop();
            _console.SpeechEngine.SpeakStop();

            //Stops all timers and clears the list.
            for (int i = 0; i < _timers.Count; i++)
            {
                _timers[i].Stop();
            }
            _timers.Clear();

            //Removes existing subscribers to OnSubmit from previous usage.
            for (int i = 0; i < _actions.Count; i++)
            {
                _console.OnSubmit -= _actions[i];
            }
            _actions.Clear();

            //Sets up variables.
            ParseNode tree;

            //Gets the nodes to process, if possible.
            _entries.TryGetValue(_fork, out tree);
            if (tree == null)
            {
                if (ShowErrors)
                {
                    throw new InterpreterException("Interpreter: fork '" +
                        _fork + "' not found.");
                }

                return;
            }

            //Evaluates every node.
            PreorderProcess(tree, String.Empty);

            //Exits if fork execution stops.
            if (_stopEvaluation)
            {
                return;
            }

            //Ensures the fork is considered to be visited.
            VisitFork();

            //If no options exist, adds an option to restart.
            if (_console.GetOptionsAmount() == 0 && _optOptionEnabled)
            {
                _console.AddOption(_optOptionDefault, new Action(() =>
                {
                    _variables.Clear();
                    SetFork(_entries.Keys.ElementAt(0));
                }));
            }
        }

        /// <summary>
        /// Crawls the given node structure in a depth-first search.
        /// </summary>
        /// <param name="node">
        /// The node to process.
        /// </param>
        /// <param name="textboxText">
        /// If text is provided, it's used solely to evaluate 'if text is' and
        /// 'if text has' syntax. Otherwise, those nodes are set to be parsed
        /// the next time input is submitted through the textbox.
        /// </param>
        private void PreorderProcess(ParseNode node, string textboxText)
        {
            if (node != null)
            {
                //If the node's conditions are met, processes it and children.
                if (ProcessIf(node, textboxText))
                {
                    ProcessText(node);

                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        if (_stopEvaluation)
                        {
                            return;
                        }

                        PreorderProcess(node.Children[i], textboxText);
                    }
                }
            }
        }

        /// <summary>
        /// Interprets the contents of a node if its condition is met.
        /// </summary>
        /// <param name="node">
        /// The node to process.
        /// </param>
        /// <param name="textboxText">
        /// If text is provided, it's used solely to evaluate 'if text is' and
        /// 'if text has' syntax. Otherwise, those nodes are set to be parsed
        /// the next time input is submitted through the textbox.
        /// </param>
        /// <returns>
        /// True if the condition is met; otherwise false.
        /// </returns>
        private bool ProcessIf(ParseNode node, string textboxText)
        {
            //If there are no conditions, consider it met.
            if (node.Condition.Trim() == String.Empty)
            {
                return true;
            }

            //Gets the condition without the word 'if'.
            string condition = node.Condition.Substring(2).Trim();
            string[] words = condition.Split(' ');

            //There should be at least one word after 'if'.
            if (words.Length == 0)
            {
                if (ShowErrors)
                {
                    throw new InterpreterException("Interpreter: The line " +
                        "'if " + condition + "' is incorrectly formatted.");
                }

                return false; //Skips ifs with invalid syntax.
            }

            #region Timers. Syntax: if timer is num
            //Handles syntax: if timer is num.
            if (words.Length > 1 && words[0] == "timer" && words[1] == "is")
            {
                double number;

                if (words.Length < 2)
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException("The timer must be " +
                            "set with a time specified in seconds.");
                    }

                    return false;
                }

                //The third word must be a number.
                if (!Double.TryParse(words[2], out number))
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: In " +
                            "line '" + condition + "', the third word must " +
                            "be numeric.");
                    }

                    return false;
                }
                if (Double.IsInfinity(number) || Double.IsNaN(number))
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: In " +
                            "line '" + condition + "', the third word must " +
                            "be numeric and not too large.");
                    }

                    return false;
                }

                //The number must be positive.
                if (number <= 0)
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: In " +
                            "line '" + condition + "', the time must be " +
                            "positive and non-zero.");
                    }

                    return false;
                }

                //Creates a timer to delay the evaluation of everything in the
                //current if-statement.
                DispatcherTimer timer = new DispatcherTimer();
                _timers.Add(timer);
                timer.Interval = TimeSpan.FromSeconds(number);
                timer.Tick += new EventHandler((a, b) =>
                    {
                        ProcessText(node);

                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            PreorderProcess(node.Children[i], textboxText);
                        }

                        timer.Stop();
                    });
                timer.Start();

                return false; //Delays execution of child nodes.
            }
            #endregion

            #region Speech. Syntax: if speak query
            //Handles syntax: if speak query.
            else if (words.Length > 1 && words[0] == "speak")
            {
                string query = String.Empty; //Contains all additional words.

                //Concatenates all words after the command syntax.
                for (int i = 1; i < words.Length; i++)
                {
                    query += words[i] + " ";
                }

                query = query.ToLower().Trim()
                    .Replace(@"\at", "@")
                    .Replace(@"\lb", "{")
                    .Replace(@"\rb", "}")
                    .Replace(@"\n", "\n")
                    .Replace(@"\s", @"\");

                if (query == String.Empty)
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: In the " +
                            "line 'if " + condition + "', at least one word " +
                            "to say aloud must be specified after 'speak'.");
                    }

                    return false;
                }

                //Evaluates nested items when the query is spoken.
                _console.SpeechEngine.Listen(new Action(() =>
                    {
                        ProcessText(node);

                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            PreorderProcess(node.Children[i], textboxText);
                        }
                    }), query);

                //If used, starts listening for input and stops spoken output.
                if (!_console.SpeechEngine.IsListening)
                {
                    _console.SpeechEngine.ListenStart();
                }

                return false; //Delays execution of child nodes.
            }
            #endregion

            #region Textbox. Syntax: if text (!)is/has/pick query
            //Handles syntax: if text is query, if text has query,
            //if text !is query, if text !has query, if text pick query
            else if (words.Length > 1 &&
                words[0] == "text" &&
                (words[1] == "is" || words[1] == "!is" ||
                words[1] == "has" || words[1] == "!has" ||
                words[1] == "pick"))
            {
                //Automatically shows the textbox.
                _console.SetInputEnabled(true);

                string query = String.Empty; //Contains all additional words.

                //Concatenates all words after the command syntax.
                for (int i = 2; i < words.Length; i++)
                {
                    query += words[i] + " ";
                }
                query = query.ToLower().Trim()
                    .Replace(@"\at", "@")
                    .Replace(@"\lb", "{")
                    .Replace(@"\rb", "}")
                    .Replace(@"\n", "\n")
                    .Replace(@"\s", @"\");

                if (query == String.Empty)
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: In the " +
                            "line 'if " + condition + "', at least one word " +
                            "to look for must be specified after 'pick'.");
                    }
                }

                //The generated option adds to the onsubmit event based on
                //whether it's checking if the textbox input is/has the query.
                if (words[1] == "pick")
                {
                    //Splits the query on commas if checking for containing.
                    string[] queryWords = query.Split(',');

                    //Escapes commas as \c.
                    for (int i = 0; i < queryWords.Length; i++)
                    {
                        queryWords[i] = queryWords[i].Replace(@"\c", ",").Trim();
                    }

                    if (textboxText == String.Empty)
                    {
                        var handler = new Console.OnSubmitHandler((sender, text) =>
                        {
                            text = text.ToLower().Trim();
                            bool containsWord = false;

                            //Ensures the text contains at least one word.
                            for (int i = 0; i < queryWords.Length; i++)
                            {
                                if (Regex.IsMatch(text, @"\b" + queryWords[i] + @"\b"))
                                {
                                    containsWord = true;
                                }
                            }

                            if (!containsWord)
                            {
                                return;
                            }

                            //If still executing, conditions are met.
                            ProcessText(node);

                            for (int i = 0; i < node.Children.Count; i++)
                            {
                                PreorderProcess(node.Children[i], text);
                            }
                        });
                        _actions.Add(handler);
                        _console.OnSubmit += handler;
                    }
                    else
                    {
                        textboxText = textboxText.ToLower().Trim();
                        bool containsWord = false;

                        //Ensures the text contains at least one word.
                        for (int i = 0; i < queryWords.Length; i++)
                        {
                            if (Regex.IsMatch(textboxText, @"\b" + queryWords[i] + @"\b"))
                            {
                                containsWord = true;
                            }
                        }

                        if (!containsWord)
                        {
                            return false;
                        }

                        //If still executing, conditions are met.
                        ProcessText(node);

                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            PreorderProcess(node.Children[i], textboxText);
                        }
                    }
                }
                else if (words[1].EndsWith("is"))
                {
                    if (textboxText == String.Empty)
                    {
                        var handler = new Console.OnSubmitHandler((sender, text) =>
                        {
                            text = text.ToLower().Trim();

                            if ((words[1] == "is" && text.Equals(query)) ||
                                (words[1] == "!is" && !text.Equals(query)))
                            {
                                ProcessText(node);

                                for (int i = 0; i < node.Children.Count; i++)
                                {
                                    PreorderProcess(node.Children[i], text);
                                }
                            }
                        });
                        _actions.Add(handler);
                        _console.OnSubmit += handler;
                    }
                    else
                    {
                        if ((words[1] == "is" && textboxText.Equals(query)) ||
                                (words[1] == "!is" && !textboxText.Equals(query)))
                        {
                            ProcessText(node);

                            for (int i = 0; i < node.Children.Count; i++)
                            {
                                PreorderProcess(node.Children[i], textboxText);
                            }
                        }
                    }
                }
                else if (words[1].EndsWith("has"))
                {
                    //Splits the query on commas if checking for containing.
                    string[] queryWords = query.Split(',');

                    //Escapes commas as \c.
                    for (int i = 0; i < queryWords.Length; i++)
                    {
                        queryWords[i] = queryWords[i].Replace(@"\c", ",");
                    }

                    if (textboxText == String.Empty)
                    {
                        var handler = new Console.OnSubmitHandler((sender, text) =>
                        {
                            text = text.ToLower().Trim();

                            //Ensures the text contains each word.
                            for (int i = 0; i < queryWords.Length; i++)
                            {
                                if (words[1] == "has" &&
                                    !Regex.IsMatch(text, @"\b" + queryWords[i] + @"\b"))
                                {
                                    return;
                                }
                                else if (words[1] == "!has" &&
                                    Regex.IsMatch(text, @"\b" + queryWords[i] + @"\b"))
                                {
                                    return;
                                }
                            }

                            //If still executing, conditions are met.
                            ProcessText(node);

                            for (int i = 0; i < node.Children.Count; i++)
                            {
                                PreorderProcess(node.Children[i], text);
                            }
                        });
                        _actions.Add(handler);
                        _console.OnSubmit += handler;
                    }
                    else
                    {
                        //Ensures the text contains each word.
                        for (int i = 0; i < queryWords.Length; i++)
                        {
                            if (words[1] == "has" &&
                                !Regex.IsMatch(textboxText, @"\b" + queryWords[i] + @"\b"))
                            {
                                return false;
                            }
                            else if (words[1] == "!has" &&
                                Regex.IsMatch(textboxText, @"\b" + queryWords[i] + @"\b"))
                            {
                                return false;
                            }
                        }

                        //If still executing, conditions are met.
                        ProcessText(node);

                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            PreorderProcess(node.Children[i], textboxText);
                        }
                    }
                }

                return false; //Execution of child nodes is conditional.
            }
            #endregion

            #region Types text. Syntax: if type ms text
            //Parses output text and escape characters.
            else if (words.Length > 2 && words[0].StartsWith("type"))
            {
                double msDelay; //Delay between letters.
                string output = String.Empty; //User text.

                //Ensures the 2nd word is a number.
                if (Double.TryParse(words[1], out msDelay))
                {
                    if (Double.IsInfinity(msDelay) || Double.IsNaN(msDelay))
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException(
                                "Interpreter: In the line '" + condition +
                                "', the number can't be too large.");
                        }

                        return false;
                    }
                }
                else
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException(
                            "Interpreter: In the line '" + condition +
                            "', the 2nd word must be a number.");
                    }

                    return false;
                }
                if (msDelay <= 0)
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException(
                            "Interpreter: In the line '" + condition +
                            "', the delay must be a positive value.");
                    }

                    return false;
                }

                //Concatenates user input into the output variable.
                for (int i = 2; i < words.Length; i++)
                {
                    output += words[i] + " ";
                }

                //Removes outer formatting and parses escapes.
                output = output.Trim()
                    .Replace(@"\at", "@")
                    .Replace(@"\lb", "{")
                    .Replace(@"\rb", "}")
                    .Replace(@"\n", "\n")
                    .Replace(@"\s", @"\");

                //Determines the style based on asterisks.
                TextStyle style = TextStyle.Normal;
                if (words[0].Equals("type*"))
                {
                    style = TextStyle.Italic;
                }
                else if (words[0].Equals("type**"))
                {
                    style = TextStyle.Bold;
                }
                else if (words[0].Equals("type***"))
                {
                    style = TextStyle.BoldItalic;
                }

                //Creates a timer that removes and prints the first
                //character in the tag every tick until done.
                DispatcherTimer timer = new DispatcherTimer();
                _timers.Add(timer);
                timer.Interval = TimeSpan.FromMilliseconds(msDelay);
                timer.Tag = output;

                timer.Tick += new EventHandler((a, b) =>
                {
                    string text = timer.Tag.ToString();

                    //Stops the timer and executes the if block when
                    //there's nothing left to process.
                    if (text.Length == 0)
                    {
                        //Handles try-catch errors separately to work around
                        //limitations of optimized code that the unoptimized
                        //code doesn't seem to have. (TODO)
                        if (_printErrors)
                        {
                            try
                            {
                                ProcessText(node);

                                for (int i = 0; i < node.Children.Count; i++)
                                {
                                    PreorderProcess(node.Children[i], textboxText);
                                }
                            }
                            catch (Exception ex)
                            {
                                _console.Clear();
                                _console.PrefOutColor = Brushes.Yellow;

                                if (ex is Parsing.ParserException ||
                                    ex is Parsing.InterpreterException)
                                {
                                    _console.AddText("\nA loading error occurred. " + ex.Message);
                                }
                                else
                                {
                                    _console.AddText("\nAn error occurred. " + ex.Message);
                                }

                                _console.PrefOutColor = Brushes.White;

                                _console.AddOption("Click to load a file.", new Action(() =>
                                {
                                    Utils.TryLoadFile(_console, String.Empty, String.Empty);
                                }));
                            }
                        }
                        else
                        {
                            ProcessText(node);

                            for (int i = 0; i < node.Children.Count; i++)
                            {
                                PreorderProcess(node.Children[i], textboxText);
                            }
                        }

                        timer.Stop();
                        return;
                    }

                    //Shortens by one character and stores it.
                    char chr = text.First();
                    timer.Tag = text.Substring(1);

                    //Generates a run for the given character.
                    Run generatedRun = new Run(chr.ToString());
                    generatedRun.Foreground = _console.PrefOutColor;
                    generatedRun.FontFamily = _console.PrefOutFontFamily;
                    generatedRun.FontSize = _console.PrefOutFontSize;

                    //Sets the styles.
                    if (style == TextStyle.Bold ||
                        style == TextStyle.BoldItalic)
                    {
                        generatedRun.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        generatedRun.FontWeight = _console.PrefOutFontWeight;
                    }
                    if (style == TextStyle.Italic ||
                        style == TextStyle.BoldItalic)
                    {
                        generatedRun.FontStyle = FontStyles.Italic;
                    }

                    _console.AddText(generatedRun);
                });

                timer.Start();

                return false;
            }
            #endregion

            #region Truth tests. Syntax: if expr; expr must be true or false.
            else
            {
                //Unregisters previously-set variables and confirms options.
                MathParsing.Parser.OptUseImplicitMult = false;
                MathParsing.Parser.OptIncludeUnknowns = true;
                MathParsing.Parser.OptUnknownDefault = new MathParsing.LiteralBool(false);
                MathParsing.Parser.ResetTokens();

                //Supports syntax: if visited, if !visited
                MathParsing.Parser.AddIdentifier(
                    new MathParsing.LiteralId("visited",
                    _variables.ContainsKey("visited" + _fork)));

                //Registers all valid variables with the math parser.
                for (int i = 0; i < _variables.Count; i++)
                {
                    var varName = _variables.Keys.ElementAt(i);
                    var varVal = _variables.Values.ElementAt(i);

                    if (varVal is decimal varValDec)
                    {
                        MathParsing.Parser.AddIdentifier(
                            new MathParsing.LiteralId(varName, varValDec));
                    }
                    else if (varVal is bool varValBool)
                    {
                        MathParsing.Parser.AddIdentifier(
                            new MathParsing.LiteralId(varName, varValBool));
                    }
                }
                //TODO: Adds "visited" as a variable.

                string result = "";
                object resultVal = null;

                //Attempts to compute the expression.
                try
                {
                    result = MathParsing.Parser.Eval(String.Join(" ", words));
                }
                catch (MathParsing.ParsingException e)
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter " + e.Message);
                    }

                    return false;
                }

                //Parses the computed result as a bool.
                if (result == "True" || result == "False")
                {
                    return bool.Parse(result);
                }
                else
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException(
                            "Interpreter: In the line 'if " +
                            String.Join(" ", words) + "', the expression must " +
                            "be boolean (true or false), but was " +
                            resultVal.GetType().ToString() + " instead.");
                    }

                    return false;
                }
            }
            #endregion
        }

        /// <summary>
        /// Interprets the node text to display output and evaluate commands.
        /// </summary>
        /// <param name="node">
        /// The node to process.
        /// </param>
        private void ProcessText(ParseNode node)
        {
            string textLeft = node.Text;

            //Processes all text until none is left.
            while (textLeft.Length > 0)
            {
                //Gets the current line and its words.
                int endOfLine = textLeft.IndexOf("\n");
                string line;

                if (endOfLine >= 0)
                {
                    line = textLeft.Substring(0, textLeft.IndexOf('\n'));
                }
                else
                {
                    line = textLeft;
                }

                string[] words = line.Split(' ');

                #region Handles empty lines if they appear.
                //Removes excess lines.
                if (line.Trim() == String.Empty)
                {
                    //Deletes pointless whitespace.
                    textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                }
                #endregion

                #region Parse in-line options. Syntax: link@ output@forkname.
                else if (textLeft.StartsWith("link@"))
                {
                    line = line.Substring(5);

                    if (line.Contains('@'))
                    {
                        //Gets the fork name. Case and space insensitive.
                        string forkName = line
                            .Substring(line.IndexOf('@') + 1)
                            .Replace(" ", String.Empty)
                            .ToLower();

                        string displayName = line
                            .Substring(0, line.IndexOf('@'))
                            .Trim()
                            .Replace(@"\at", "@")
                            .Replace(@"\n", "\n")
                            .Replace(@"\s", @"\");

                        //Handles having no hyperlink or display name.
                        if (forkName.Trim() == String.Empty && ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                                "there was no fork name given to option '" +
                                displayName + "'.");
                        }
                        else if (displayName.Trim() == String.Empty && ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                                "the option linking to '" + forkName + "' has no " +
                                "displayable text specified.");
                        }
                        else if (!_entries.Keys.Contains(forkName) && ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                                "The fork in the option '" + displayName +
                                "@" + forkName + "' doesn't exist.");
                        }
                        else
                        {
                            //Creates a hyperlink option for the console's
                            //output text rather than options.
                            Run generatedRun = new Run(displayName);
                            generatedRun.Foreground = _console.PrefOptColor;
                            generatedRun.FontFamily = _console.PrefOptFontFamily;
                            generatedRun.FontSize = _console.PrefOptFontSize;
                            generatedRun.FontWeight = _console.PrefOptFontWeight;

                            //Sets the color.

                            //If the in-line options should be styled like
                            //options, captures colors to local variables to
                            //keep them constant.
                            if (_optLinkStyleHidden)
                            {
                                generatedRun.Foreground = _console.PrefOutColor;
                            }
                            else
                            {
                                Brush optHgltColor = _console.PrefOptHighlightColor;
                                Brush optColor = _console.PrefOptColor;

                                //Changes color in response to mouse.
                                generatedRun.MouseEnter += (a, b) =>
                                {
                                    generatedRun.Foreground = optHgltColor;
                                };
                                generatedRun.MouseLeave += (a, b) =>
                                {
                                    generatedRun.Foreground = optColor;
                                };
                            }

                            //Responds to clicks.
                            generatedRun.MouseLeftButtonDown += (a, b) =>
                            {
                                SetFork(forkName);
                            };

                            //Adds to options.
                            _console.AddText(generatedRun);
                        }
                    }
                    else
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                                "In the line '" + line + "', an option " +
                                "must be specified.");
                        }
                    }

                    //Deletes the line just processed.
                    textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                }
                #endregion

                #region Parse options. Syntax: output@forkname.
                //Parses options.
                else if (line.Contains('@'))
                {
                    //Gets the fork name. Case and space insensitive.
                    string forkName = line
                        .Substring(line.IndexOf('@') + 1)
                        .Replace(" ", String.Empty)
                        .ToLower();

                    string displayName = line
                        .Substring(0, line.IndexOf('@'))
                        .Trim()
                        .Replace(@"\at", "@")
                        .Replace(@"\n", "\n")
                        .Replace(@"\s", @"\");

                    //Handles having no hyperlink or display name.
                    if (forkName.Trim() == String.Empty && ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: " +
                            "there was no fork name given to option '" +
                            displayName + "'.");
                    }
                    else if (displayName.Trim() == String.Empty && ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: " +
                            "the option linking to '" + forkName + "' has no " +
                            "displayable text specified.");
                    }
                    else if (!_entries.Keys.Contains(forkName) && ShowErrors)
                    {
                        throw new InterpreterException("The fork in the " +
                            "option '" + displayName + "@" + forkName + "' " +
                            "doesn't exist.");
                    }
                    else
                    {
                        //When clicked, the option changes the fork.
                        _console.AddOption(displayName, new Action(() =>
                        {
                            SetFork(forkName);
                        }));
                    }

                    //Deletes the line just processed.
                    textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                }
                #endregion

                #region Print text. Syntax: {output text}.
                //Parses output text and escape characters.
                else if (line.Contains('{'))
                {
                    int lbPos = textLeft.IndexOf('{');
                    int rbPos = textLeft.IndexOf('}');
                    string output = textLeft
                        .Substring(lbPos, rbPos - lbPos + 1);

                    TextStyle style = TextStyle.Normal;
                    if (output.Contains("***}"))
                    {
                        style = TextStyle.BoldItalic;
                    }
                    else if (output.Contains("**}"))
                    {
                        style = TextStyle.Bold;
                    }
                    else if (output.Contains("*}"))
                    {
                        style = TextStyle.Italic;
                    }

                    //Removes outer formatting and parses escapes.
                    output = output
                        .Replace("{", String.Empty)
                        .Replace("***}", String.Empty)
                        .Replace("**}", String.Empty)
                        .Replace("*}", String.Empty)
                        .Replace("}", String.Empty)
                        .Replace(@"\at", "@")
                        .Replace(@"\lb", "{")
                        .Replace(@"\rb", "}")
                        .Replace(@"\n", "\n")
                        .Replace(@"\s", @"\");

                    //Generates the text options, changing bold/italicness.
                    Run generatedRun = new Run(output);
                    generatedRun.Foreground = _console.PrefOutColor;
                    generatedRun.FontFamily = _console.PrefOutFontFamily;
                    generatedRun.FontSize = _console.PrefOutFontSize;

                    if (style == TextStyle.Bold ||
                        style == TextStyle.BoldItalic)
                    {
                        generatedRun.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        generatedRun.FontWeight = _console.PrefOutFontWeight;
                    }
                    if (style == TextStyle.Italic ||
                        style == TextStyle.BoldItalic)
                    {
                        generatedRun.FontStyle = FontStyles.Italic;
                    }

                    //Adds the text.
                    _console.AddText(generatedRun);

                    //Removes the processed text.
                    textLeft = textLeft.Remove(lbPos, rbPos - lbPos + 1);
                }
                #endregion

                #region Types text. Syntax: type ms text.
                //Parses output text and escape characters.
                else if (textLeft.StartsWith("type"))
                {
                    if (words.Length > 2)
                    {
                        double msDelay; //Delay between letters.
                        string output = String.Empty; //User text.

                        //Ensures the 2nd word is a number.
                        if (Double.TryParse(words[1], out msDelay))
                        {
                            if (Double.IsInfinity(msDelay) || Double.IsNaN(msDelay))
                            {
                                if (ShowErrors)
                                {
                                    throw new InterpreterException(
                                        "Interpreter: In the line '" + line +
                                        "', the number can't be too large.");
                                }

                                //Deletes the line being processed.
                                if (endOfLine >= 0)
                                {
                                    textLeft = textLeft
                                        .Substring(textLeft.IndexOf('\n') + 1);
                                }
                                else
                                {
                                    textLeft = String.Empty;
                                }

                                continue;
                            }
                        }
                        else
                        {
                            if (ShowErrors)
                            {
                                throw new InterpreterException(
                                    "Interpreter: In the line '" + line +
                                    "', the 2nd word must be a number.");
                            }

                            //Deletes the line being processed.
                            if (endOfLine >= 0)
                            {
                                textLeft = textLeft
                                    .Substring(textLeft.IndexOf('\n') + 1);
                            }
                            else
                            {
                                textLeft = String.Empty;
                            }

                            continue;
                        }
                        if (msDelay <= 0)
                        {
                            if (ShowErrors)
                            {
                                throw new InterpreterException(
                                    "Interpreter: In the line '" + line +
                                    "', the delay must be a positive value.");
                            }

                            //Deletes the line being processed.
                            if (endOfLine >= 0)
                            {
                                textLeft = textLeft
                                    .Substring(textLeft.IndexOf('\n') + 1);
                            }
                            else
                            {
                                textLeft = String.Empty;
                            }

                            continue;
                        }

                        //Concatenates user input into the output variable.
                        for (int i = 2; i < words.Length; i++)
                        {
                            output += words[i] + " ";
                        }

                        //Removes outer formatting and parses escapes.
                        output = output.Trim()
                            .Replace(@"\at", "@")
                            .Replace(@"\lb", "{")
                            .Replace(@"\rb", "}")
                            .Replace(@"\n", "\n")
                            .Replace(@"\s", @"\");

                        //Determines the style based on asterisks.
                        TextStyle style = TextStyle.Normal;
                        if (words[0].Equals("type*"))
                        {
                            style = TextStyle.Italic;
                        }
                        else if (words[0].Equals("type**"))
                        {
                            style = TextStyle.Bold;
                        }
                        else if (words[0].Equals("type***"))
                        {
                            style = TextStyle.BoldItalic;
                        }

                        //Creates a timer that removes and prints the first
                        //character in its string every tick until done.
                        DispatcherTimer timer = new DispatcherTimer();
                        _timers.Add(timer);
                        timer.Interval = TimeSpan.FromMilliseconds(msDelay);
                        timer.Tag = output;

                        timer.Tick += new EventHandler((a, b) =>
                        {
                            string text = timer.Tag.ToString();

                            //Stops the timer when there's nothing left.
                            if (text.Length == 0)
                            {
                                timer.Stop();
                                return;
                            }

                            //Shortens by one character and stores it.
                            char chr = text.First();
                            timer.Tag = text.Substring(1);

                            //Generates a run for the given character.
                            Run generatedRun = new Run(chr.ToString());
                            generatedRun.Foreground = _console.PrefOutColor;
                            generatedRun.FontFamily = _console.PrefOutFontFamily;
                            generatedRun.FontSize = _console.PrefOutFontSize;

                            //Sets the styles.
                            if (style == TextStyle.Bold ||
                                style == TextStyle.BoldItalic)
                            {
                                generatedRun.FontWeight = FontWeights.Bold;
                            }
                            else
                            {
                                generatedRun.FontWeight = _console.PrefOutFontWeight;
                            }
                            if (style == TextStyle.Italic ||
                                style == TextStyle.BoldItalic)
                            {
                                generatedRun.FontStyle = FontStyles.Italic;
                            }

                            _console.AddText(generatedRun);
                        });

                        timer.Start();
                    }
                    else if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', a typing effect " +
                            "must be followed by the speed (slow, medium, " +
                            "or fast), and the text to type.");
                    }

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                #region Set variables.
                else if (textLeft.StartsWith("set"))
                {
                    //Unregisters previously-set variables.
                    MathParsing.Parser.OptUseImplicitMult = false;
                    MathParsing.Parser.OptIncludeUnknowns = false;
                    MathParsing.Parser.ResetTokens();

                    //Registers all valid variables with the math parser.
                    for (int i = 0; i < _variables.Count; i++)
                    {
                        var varName = _variables.Keys.ElementAt(i);
                        var varVal = _variables.Values.ElementAt(i);

                        if (varVal is decimal varValDec)
                        {
                            MathParsing.Parser.AddIdentifier(
                                new MathParsing.LiteralId(varName, varValDec));
                        }
                        else if (varVal is bool varValBool)
                        {
                            MathParsing.Parser.AddIdentifier(
                                new MathParsing.LiteralId(varName, varValBool));
                        }
                    }

                    //Gets the index to separate left and right-hand sides.
                    int exprTwoSidedIndex = Array.IndexOf(words, "=");

                    //Handles expressions with both LHS and RHS.
                    if (exprTwoSidedIndex != -1)
                    {
                        string[] lhs = words.Skip(1).Take(exprTwoSidedIndex - 1).ToArray();
                        string[] rhs = words.Skip(exprTwoSidedIndex + 1).ToArray();
                        string result = "";
                        object resultVal = null;

                        //If the left-hand side is a single word.
                        if (lhs.Length == 1)
                        {
                            //Attempts to compute the RHS expression.
                            try
                            {
                                result = MathParsing.Parser.Eval(String.Join(" ", rhs));
                            }
                            catch (MathParsing.ParsingException e)
                            {
                                if (ShowErrors)
                                {
                                    throw new InterpreterException(
                                        "Interpreter: In the line '" + line +
                                        "', " + e.Message);
                                }
                            }

                            //Parses the computed result as a bool.
                            if (result == "True" || result == "False")
                            {
                                resultVal = bool.Parse(result);
                            }

                            //Parses the computed result as a decimal.
                            else
                            {
                                if (Decimal.TryParse(result, out decimal resultValDec))
                                {
                                    resultVal = resultValDec;
                                }
                                else
                                {
                                    if (ShowErrors)
                                    {
                                        throw new InterpreterException(
                                            "Interpreter: In the line '" + line +
                                            "', the expression " + String.Join(" ", rhs) +
                                            " should be a number, but " + result +
                                            " was computed instead.");
                                    }
                                }
                            }

                            //Sets or adds the new value as appropriate.
                            if (_variables.ContainsKey(lhs[0]))
                            {
                                _variables[lhs[0]] = resultVal;
                            }
                            else
                            {
                                if (char.IsDigit(lhs[0][0]) ||
                                    MathParsing.Parser.GetTokens().Any((o) => o.StrForm == lhs[0]))
                                {
                                    if (ShowErrors)
                                    {
                                        throw new InterpreterException(
                                            "Interpreter: In the line '" + line +
                                            "', the variable '" + lhs[0] +
                                            "' is a name used for math or is a number.");
                                    }
                                }
                                else
                                {
                                    _variables.Add(lhs[0], resultVal);
                                }
                            }
                        }
                        else
                        {
                            if (ShowErrors)
                            {
                                throw new InterpreterException(
                                    "Interpreter: In the line '" + line +
                                    "', the phrase " + String.Join(" ", lhs) +
                                    " must be a variable name without spaces.");
                            }
                        }
                    }

                    //Handles shorthand expressions with only the LHS.
                    else
                    {
                        string[] lhs = words.Skip(1).ToArray();
                        string result = "";
                        object resultVal = null;

                        if (lhs.Length > 0)
                        {
                            //Syntax: set name, set !name
                            if (lhs.Length == 1)
                            {
                                //Sets false boolean values.
                                if (lhs[0].StartsWith("!"))
                                {
                                    string lhsBool = lhs[0].Substring(1);

                                    if (_variables.ContainsKey(lhsBool))
                                    {
                                        _variables[lhsBool] = false;
                                    }
                                    else if (lhs.Length > 0 && (char.IsDigit(lhs[0][0]) ||
                                        MathParsing.Parser.GetTokens().Any((o) => o.StrForm == lhsBool)))
                                    {
                                        if (ShowErrors)
                                        {
                                            throw new InterpreterException(
                                                "Interpreter: In the line '" + line +
                                                "', the variable '" + lhsBool +
                                                "' is a number or is used for math.");
                                        }
                                    }
                                    else
                                    {
                                        _variables.Add(lhsBool, false);
                                    }
                                }

                                //Sets true boolean values.
                                else
                                {
                                    if (_variables.ContainsKey(lhs[0]))
                                    {
                                        _variables[lhs[0]] = true;
                                    }
                                    else if (lhs.Length > 0 && (char.IsDigit(lhs[0][0]) ||
                                        MathParsing.Parser.GetTokens().Any((o) => o.StrForm == lhs[0])))
                                    {
                                        if (ShowErrors)
                                        {
                                            throw new InterpreterException(
                                                "Interpreter: In the line '" + line +
                                                "', the variable '" + lhs[0] +
                                                "' is a name used for math or is a number.");
                                        }
                                    }
                                    else
                                    {
                                        _variables.Add(lhs[0], true);
                                    }
                                }
                            }

                            //Syntax: set EXPR, where EXPR is a math expression and not equation.
                            //This is computed as set name = EXPR.
                            else if (_variables.ContainsKey(lhs[0]))
                            {
                                //Attempts to compute the LHS expression.
                                try
                                {
                                    result = MathParsing.Parser.Eval(String.Join(" ", lhs));
                                }
                                catch (MathParsing.ParsingException e)
                                {
                                    if (ShowErrors)
                                    {
                                        throw new InterpreterException(
                                            "Interpreter: In the line '" + line +
                                            "', " + e.Message);
                                    }
                                }

                                //Parses the computed result as a bool.
                                if (result == "True" || result == "False")
                                {
                                    resultVal = bool.Parse(result);
                                }

                                //Parses the computed result as a decimal.
                                else
                                {
                                    if (Decimal.TryParse(result, out decimal resultValDec))
                                    {
                                        resultVal = resultValDec;
                                    }
                                    else
                                    {
                                        if (ShowErrors)
                                        {
                                            throw new InterpreterException(
                                                "Interpreter: In the line '" + line +
                                                "', the expression " + String.Join(" ", lhs) +
                                                " should be a number, but " + result +
                                                " was computed instead.");
                                        }
                                    }
                                }

                                _variables[lhs[0]] = resultVal;
                            }
                            else
                            {
                                if (ShowErrors)
                                {
                                    throw new InterpreterException(
                                        "Interpreter: In the line '" + line +
                                        "', the variable " + lhs[0] + " doesn't " +
                                        "exist yet.");
                                }
                            }
                        }
                        else
                        {
                            if (ShowErrors)
                            {
                                throw new InterpreterException(
                                    "Interpreter: In the line '" + line +
                                    "', you need to provide a variable name to " +
                                    "set, using syntax like set a, set !a, or " +
                                    "a mathematical expression.");
                            }
                        }
                    }

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                #region Print variables. Syntax: get name.
                //Syntax: get name.
                else if (textLeft.StartsWith("get"))
                {
                    if (words.Length == 2)
                    {
                        if (_variables.ContainsKey(words[1]))
                        {
                            _console.AddText(_variables[words[1]].ToString());
                        }
                        else if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', variable " +
                            words[1] + " does not exist.");
                        }
                    }
                    else if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', only one word can " +
                            "follow 'get'. ");
                    }

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                #region Goto other forks. Syntax: goto forkname.
                //Handles syntax: goto forkname.
                else if (textLeft.StartsWith("goto"))
                {
                    string forkName = line
                        .Substring(4)
                        .Replace(" ", String.Empty)
                        .ToLower()
                        .Trim();

                    if (_entries.ContainsKey(forkName))
                    {
                        //Ensures this page is considered visited, then
                        //executes the page being jumped to. When execution
                        //flow returns, this exits out of everything.
                        VisitFork();
                        SetFork(forkName);
                        _stopEvaluation = true;
                        return;
                    }
                    else
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                                "In the line '" + textLeft + " ', cannot " +
                                "navigate to fork '" + forkName +
                                "' because it does not exist.");
                        }
                    }

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                #region Load images. Syntax: img url.
                //Syntax: img url.
                else if (textLeft.StartsWith("img"))
                {
                    //Gets the url.
                    string url = String.Empty;
                    for (int i = 1; i < words.Length; i++)
                    {
                        url += words[i];
                    }
                    url = url.Trim()
                        .Replace(@"\at", "@")
                        .Replace(@"\lb", "{")
                        .Replace(@"\rb", "}")
                        .Replace(@"\n", "\n")
                        .Replace(@"\s", @"\");

                    if (url == String.Empty)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', 'img' must be " +
                            "followed by an image location relative to the " +
                            "loaded file's directory.");
                        }
                    }

                    url = Path.GetFullPath(
                        Path.Combine(Utils.Url, url));

                    //If the file exists, attempts to load the image.
                    if (File.Exists(url))
                    {
                        try
                        {
                            _console.AddImage(url, true);
                        }
                        catch
                        {
                            if (ShowErrors)
                            {
                                throw new InterpreterException(
                                    "Interpreter: Image given on line '" +
                                    line + "' is unsupported, corrupted, " +
                                    "or too large.");
                            }
                        }
                    }
                    else
                    {
                        throw new InterpreterException(
                            "Interpreter: Image given on line '" +
                            line + "' could not be found.");
                    }

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                #region Load sounds. Syntax: snd url.
                //Handles syntax: snd url.
                else if (textLeft.StartsWith("snd"))
                {
                    //Gets the url.
                    string url = String.Empty;
                    for (int i = 1; i < words.Length; i++)
                    {
                        url += words[i];
                    }
                    url = url.Trim()
                        .Replace(@"\at", "@")
                        .Replace(@"\lb", "{")
                        .Replace(@"\rb", "}")
                        .Replace(@"\n", "\n")
                        .Replace(@"\s", @"\");

                    if (url == String.Empty)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', 'snd' must be " +
                            "followed by a sound location relative to the " +
                            "loaded file's directory.");
                        }
                    }

                    url = Path.GetFullPath(
                        Path.Combine(Utils.Url, url));

                    //If the file exists, attempts to load the sound.
                    if (File.Exists(url))
                    {
                        try
                        {
                            //Creates a soundplayer to play the sound.
                            SoundPlayer snd = new SoundPlayer(url);
                            snd.Play();
                            snd.Dispose();
                        }
                        catch
                        {
                            if (ShowErrors)
                            {
                                throw new InterpreterException(
                                    "Interpreter: Sound given on line '" +
                                    line + "' is unsupported, corrupted, " +
                                    "or too long.");
                            }
                        }
                    }
                    else
                    {
                        throw new InterpreterException(
                        "Interpreter: Sound given on line '" +
                        line + "' could not be found.");
                    }

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                #region Speak. Syntax: speak query.
                //Handles syntax: speak query.
                else if (textLeft.StartsWith("speak"))
                {
                    //Gets the query.
                    string query = String.Empty;
                    for (int i = 1; i < words.Length; i++)
                    {
                        query += words[i] + " ";
                    }

                    //It's almost pointless to support escapes,
                    //but they're supported here for consistency.
                    query = query.Trim()
                        .Replace(@"\at", "@")
                        .Replace(@"\lb", "{")
                        .Replace(@"\rb", "}")
                        .Replace(@"\n", "\n")
                        .Replace(@"\s", @"\");

                    if (query == String.Empty)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', 'speak' must be " +
                            "followed by text to be spoken aloud.");
                        }
                    }

                    //Says the query.
                    _console.SpeechEngine.Speak(query);

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                #region Jump to files. Syntax: load url, load new url.
                //Syntax: load url, load new url.
                else if (textLeft.StartsWith("load"))
                {
                    //Gets the url.
                    string url = String.Empty;

                    for (int i = 1; i < words.Length; i++)
                    {
                        url += words[i] + " ";
                    }
                    url = url.Trim();

                    //Adjusts the url and drops variables.
                    if (textLeft.StartsWith("load new"))
                    {
                        _variables.Clear();
                        url = url.Substring(3); //removes 'new'.
                    }

                    if (url == String.Empty)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', 'load' must be " +
                            "followed by a file location relative to the " +
                            "loaded file's directory.");
                        }
                    }

                    url = Path.GetFullPath(
                        Path.Combine(Utils.Url, url));

                    //If the file exists, attempts to load the file.
                    if (File.Exists(url))
                    {
                        try
                        {
                            Parser.LoadFile(url, this, String.Empty);
                            return;
                        }
                        catch
                        {
                            if (ShowErrors)
                            {
                                throw new InterpreterException(
                                    "Interpreter: Text file given on line '" +
                                    line + "' is unsupported or corrupted.");
                            }
                        }
                    }
                    else
                    {
                        throw new InterpreterException(
                        "Interpreter: Text file given on line '" +
                        line + "' could not be found.");
                    }

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                #region Set text color. Syntax: color ffffff, color fff.
                //Handles syntax: color ffffff (and other hex codes).
                else if (textLeft.StartsWith("color"))
                {
                    string color = line.Substring(5).ToLower().Trim();
                    if (color != Regex.Match(color, @"[0123456789abcdef]+").Value)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', color must be " +
                            "given in hex format. It can only include " +
                            "numbers 1-9 and uppercase or lowercase a-f.");
                        }
                    }
                    else if (color.Length != 6 && color.Length != 3)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', color must be " +
                            "given in hex format using 3 or 6 digits. For " +
                            "example, f00 or 8800f0.");
                        }
                    }
                    else if (color.Length == 3)
                    {
                        //Gets the rgb components.
                        string red, grn, blu;
                        red = color.Substring(0, 1);
                        grn = color.Substring(1, 1);
                        blu = color.Substring(2, 1);

                        //Sets the new font color.
                        _console.PrefOutColor = new SolidColorBrush(
                            Color.FromRgb(
                                Convert.ToByte(red + red, 16),
                                Convert.ToByte(grn + grn, 16),
                                Convert.ToByte(blu + blu, 16)));
                    }
                    else if (color.Length == 6)
                    {
                        //Gets the rgb components.
                        string red, grn, blu;
                        red = color.Substring(0, 2);
                        grn = color.Substring(2, 2);
                        blu = color.Substring(4, 2);

                        //Sets the new font color.
                        _console.PrefOutColor = new SolidColorBrush(
                            Color.FromRgb(
                                Convert.ToByte(red, 16),
                                Convert.ToByte(grn, 16),
                                Convert.ToByte(blu, 16)));
                    }

                    //Deletes the line just processed.
                    if (endOfLine >= 0)
                    {
                        textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                    }
                    else
                    {
                        textLeft = String.Empty;
                    }
                }
                #endregion

                //Anything left is an error.
                else
                {
                    if (ShowErrors)
                    {
                        throw new InterpreterException("Interpreter: " +
                        "In the line '" + line + "', unexpected symbols " +
                        "encountered. Ensure all output text is wrapped " +
                        "in single braces and there are no extra braces " +
                        "inside.");
                    }

                    //Skips the unprocessable line.
                    textLeft = textLeft.Substring(textLeft.IndexOf('\n') + 1);
                }
            }
        }

        /// <summary>
        /// Automatically sets variables to indicate pages were visited.
        /// </summary>
        private void VisitFork()
        {
            //Called when a fork is finished executing or is stopped so
            //another fork can run, in which this should execute
            //immediately.
            if (!_variables.ContainsKey("visited" + _fork))
            {
                _variables.Add("visited" + _fork, true);
            }
        }

        /// <summary>
        /// Processes game options specified at the top of game files.
        /// </summary>
        /// <param name="text">
        /// The header text.
        /// </param>
        public void ProcessHeaderOptions(string text)
        {
            //Clears all old preferences.
            _console.ResetPreferences();

            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                //Gets the line and words on that line.
                string line = lines[i];
                string[] words = line.Split(' ');

                //Gets all text after the option has been named.
                string input = String.Empty;
                for (int j = 1; j < words.Length; j++)
                {
                    input += words[j] + " ";
                }
                input = input.Trim();

                if (line.StartsWith("link-style-text"))
                {
                    _optLinkStyleHidden = true;
                }

                else if (line.StartsWith("option-default-text"))
                {
                    _optOptionDefault = input;
                }

                else if (line.StartsWith("option-default-disable"))
                {
                    _optOptionEnabled = false;
                }

                else if (line.StartsWith("option-color") ||
                    line.StartsWith("option-hover-color") ||
                    line.StartsWith("background-color"))
                {
                    //Stores the color to be created.
                    SolidColorBrush color = new SolidColorBrush();

                    if (input != Regex.Match(input, @"[0123456789abcdefABCDEF]+").Value)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', color must be " +
                            "given in hex format. It can only include " +
                            "numbers 1-9 and uppercase or lowercase a-f.");
                        }
                    }
                    else if (input.Length != 6 && input.Length != 3)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: " +
                            "In the line '" + line + "', color must be " +
                            "given in hex format using 3 or 6 digits. For " +
                            "example, f00 or 8800f0.");
                        }
                    }
                    else if (input.Length == 3)
                    {
                        //Gets the rgb components.
                        string red, grn, blu;
                        red = input.Substring(0, 1);
                        grn = input.Substring(1, 1);
                        blu = input.Substring(2, 1);

                        //Sets the new font color.
                        color = new SolidColorBrush(
                            Color.FromRgb(
                                Convert.ToByte(red + red, 16),
                                Convert.ToByte(grn + grn, 16),
                                Convert.ToByte(blu + blu, 16)));
                    }
                    else if (input.Length == 6)
                    {
                        //Gets the rgb components.
                        string red, grn, blu;
                        red = input.Substring(0, 2);
                        grn = input.Substring(2, 2);
                        blu = input.Substring(4, 2);

                        //Sets the new font color.
                        color = new SolidColorBrush(
                            Color.FromRgb(
                                Convert.ToByte(red, 16),
                                Convert.ToByte(grn, 16),
                                Convert.ToByte(blu, 16)));
                    }

                    if (line.StartsWith("option-color"))
                    {
                        _console.PrefOptColor = color;
                    }
                    else if (line.StartsWith("option-hover-color"))
                    {
                        _console.PrefOptHighlightColor = color;
                    }
                    else if (line.StartsWith("background-color"))
                    {
                        _console.PrefBackColor = color;
                    }
                }
                else if (line.StartsWith("output-font-size") ||
                    line.StartsWith("option-font-size") ||
                    (line.StartsWith("window-width") ||
                    line.StartsWith("window-height")))
                {
                    double number;

                    if (Double.TryParse(input, out number))
                    {
                        if (Double.IsInfinity(number) || Double.IsNaN(number))
                        {
                            if (ShowErrors)
                            {
                                throw new InterpreterException("Interpreter: In " +
                                    "line '" + line + "', the number " +
                                    "can't be too large.");
                            }

                            continue;
                        }
                    }
                    else
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: In " +
                                "line '" + line + "', a number must be " +
                                "specified after the option.");
                        }

                        continue;
                    }
                    if (number <= 0)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: In " +
                                "line '" + line + "', numbers must be " +
                                "greater than zero.");
                        }

                        continue;
                    }
                    if ((line.StartsWith("window-width") ||
                        line.StartsWith("window-height")) &&
                        (int)number != number)
                    {
                        if (ShowErrors)
                        {
                            throw new InterpreterException("Interpreter: In " +
                                "line '" + line + "', numbers must be " +
                                "integers.");
                        }

                        continue;
                    }

                    if (line.StartsWith("output-font-size"))
                    {
                        _console.PrefOutFontSize = number;
                    }
                    else if (line.StartsWith("option-font-size"))
                    {
                        _console.PrefOptFontSize = number;
                    }
                    else if (line.StartsWith("window-width"))
                    {
                        _console.SetWidth((int)number);
                    }
                    else if (line.StartsWith("window-height"))
                    {
                        _console.SetHeight((int)number);
                    }
                }
                else if (line.StartsWith("option-font"))
                {
                    _console.PrefOptFontFamily =
                        new FontFamily(input + ", Arial, Helvetica, Sans Serif");
                }
                else if (line.StartsWith("output-font"))
                {
                    _console.PrefOutFontFamily =
                        new FontFamily(input + ", Arial, Helvetica, Sans Serif");
                }
            }
        }

        /// <summary>
        /// Represents different styles of text.
        /// </summary>
        private enum TextStyle
        {
            Normal,
            Bold,
            Italic,
            BoldItalic
        }
        #endregion
    }
}