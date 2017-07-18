using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AdventureText.Parsing
{
    /// <summary>
    /// Performs the initial separation of a text file into separate tokenized
    /// segments. These segments are stored in an array by name and sent to
    /// the interpreter when requested for loading.
    /// </summary>
    class Parser
    {

        #region Members
        /// <summary>
        /// Errors will be shown if true.
        /// </summary>
        private static bool _showErrors = false;
        #endregion

        #region Properties
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
        #endregion

        #region Methods
        /// <summary>
        /// Loads a file in a format matching the parsing syntax.
        /// </summary>
        /// <exception cref="ParserException">
        /// Thrown for mismatched or incorrect numbers of if-endif pairs.
        /// Thrown when the file to be read cannot be resolved or read.
        /// </exception>
        /// <returns>
        /// A list of trees, where each entry is its own tree.
        /// </returns>
        public static void LoadFile(string path,
            Interpreter interpreter, string forkToLoad)
        {
            #region Split text file into separate entries
            //Gets text from a file and creates a dictionary to store forks.
            List<string> fileText;
            try
            {
                fileText = File.ReadAllLines(path).ToList();
            }
            catch (Exception)
            {
                if (ShowErrors)
                {
                    throw new ParserException("Parser: File \"" +
                        Path.Combine(Environment.CurrentDirectory, path) +
                        "\" does not exist or cannot be read.");
                }

                interpreter.SetEntries(new Dictionary<string, ParseNode>());
                return;
            }

            if (fileText.Count == 0)
            {
                if (ShowErrors)
                {
                    throw new ParserException("Parser: File is empty. " +
                        "Files must contain text to load correctly.");
                }

                interpreter.SetEntries(new Dictionary<string, ParseNode>());
                return;
            }

            var entries = new Dictionary<string, string>();
            var parsed = new Dictionary<string, ParseNode>();

            //Finds fork header positions, normalizes line endings, and
            //removes excess space.
            List<int> entryPositions = new List<int>();
            for (int i = 0; i < fileText.Count; i++)
            {
                fileText[i] = fileText[i].Replace("\r", String.Empty).Trim();

                if (fileText[i].StartsWith("@"))
                {
                    entryPositions.Add(i);
                }
            }

            //Interprets all text up to the first header as game options.
            string header = "";
            for (int i = 0; i < entryPositions.FirstOrDefault(); i++)
            {
                header += fileText[i] + "\n";
            }

            interpreter.ProcessHeaderOptions(header);

            //Splits entries into a dictionary.
            for (int i = 0; i < entryPositions.Count; i++)
            {
                //Prevents unnamed entries.
                if (fileText[entryPositions[i]].Length < 2)
                {
                    if (ShowErrors)
                    {
                        throw new ParserException("Parser: Entry '" +
                            fileText[entryPositions[i]] + "' must be at " +
                            "least 1 character long.");
                    }

                    continue;
                }

                //Stores the fork header name, content, and content by line.
                string entryName = fileText[entryPositions[i]].Substring(1);
                List<string> entryList = new List<string>();
                string entry = String.Empty;
                
                //Associates forks with their content.
                if (i == entryPositions.Count - 1)
                {
                    entryList = fileText.GetRange(
                        entryPositions[i],
                        fileText.Count - entryPositions[i]);
                }
                else
                {
                    entryList = fileText.GetRange(
                        entryPositions[i],
                        entryPositions[i + 1] - entryPositions[i]);
                }

                //Concatenates each line of text.
                for (int j = 1; j < entryList.Count; j++)
                {
                    entry += entryList[j] + "\n";
                }

                entryName = Regex.Replace(entryName, @"\s+", String.Empty).ToLower();
                if (entries.ContainsKey(entryName))
                {
                    if (ShowErrors)
                    {
                        throw new ParserException("Parser: The entry '" +
                            entryName + "' is redundant (already exists).");
                    }

                    continue;
                }
                else
                {
                    entries.Add(entryName, entry);
                }
            }
            #endregion

            #region Remove comments per entry
            //Removes single-line comments from entries.
            for (int i = 0; i < entries.Values.Count; i++)
            {
                string entry = entries.Values.ElementAt(i);
                bool isFinished;

                do
                {
                    isFinished = true;
                    MatchCollection candidates = Regex.Matches(entry, @"//");

                    //Determines if candidates are output text or commands.
                    for (int j = 0; j < candidates.Count; j++)
                    {
                        int pos = candidates[j].Index;

                        //Real comments are removed.
                        if (!IsOutput(
                            entry,
                            pos,
                            candidates[j].Length) &&
                            !IsOption(
                            entry,
                            pos,
                            candidates[j].Length))
                        {
                            entry = entry.Remove(
                                pos,
                                entry.Substring(pos).IndexOf("\n"));

                            //Comment indices are invalidated. Search again as
                            //long as comments might exist (until all found //
                            //instances are output text).
                            isFinished = false;
                            candidates = Regex.Matches(entry, @"//");
                            break;
                        }
                    }
                } while (!isFinished);

                //Sets the entry.
                entries[entries.Keys.ElementAt(i)] = entry;
            }
            #endregion

            #region Construct parse trees per entry
            //Creates a parse tree.
            for (int i = 0; i < entries.Values.Count; i++)
            {
                //Sets a node tree and a node to act as a pointer.
                ParseNode root = new ParseNode();
                ParseNode node = root;
                int depth = 0;

                //The full entry.
                string text = entries.Values.ElementAt(i);

                //Finds all if and endif words.
                MatchCollection ifMatches = Regex.Matches(text, @"\bif\b");
                MatchCollection endifMatches = Regex.Matches(text, @"\bendif\b");
                List<int> ifs = new List<int>();
                List<int> endifs = new List<int>();

                //Filters out if and endif words that are output text.
                for (int j = 0; j < ifMatches.Count; j++)
                {
                    if (!IsOutput(
                        text,
                        ifMatches[j].Index, 
                        ifMatches[j].Length)&&
                        !IsOption(
                        text,
                        ifMatches[j].Index,
                        ifMatches[j].Length))
                    {
                        ifs.Add(ifMatches[j].Index);
                    }
                }
                for (int j = 0; j < endifMatches.Count; j++)
                {
                    if (!IsOutput(
                        text,
                        endifMatches[j].Index,
                        endifMatches[j].Length)&&
                        !IsOption(
                        text,
                        endifMatches[j].Index,
                        endifMatches[j].Length))
                    {
                        endifs.Add(endifMatches[j].Index);
                    }
                }

                //Ensures the number of if and endif statements match.
                if (ifs.Count != endifs.Count)
                {
                    if (ShowErrors)
                    {
                        throw new ParserException("Parser: found " +
                            ifs.Count + " if tokens, but " + endifs.Count +
                            " endif " + "tokens. Ifs and endifs must match " +
                            "in number.");
                    }
                }

                //Creates a list of all if and endif statements by index,
                //where ifs are encoded by 0 and endifs by 1.
                var allMatches = new List<Tuple<int, int>>();
                for (int j = 0; j < ifs.Count; j++)
                {
                    allMatches.Add(new Tuple<int, int>(ifs[j], 0));
                }
                for (int j = 0; j < endifs.Count; j++)
                {
                    allMatches.Add(new Tuple<int, int>(endifs[j], 1));
                }

                //Orders all ifs and endifs in ascending order by index.
                allMatches = allMatches.OrderBy(x => x.Item1).ToList();

                //Iterates over all ifs and endifs to create a tree.
                for (int j = 0; j < allMatches.Count; j++)
                {
                    //The index, condition, and type (if, endif) of the match.
                    int elemBegin = allMatches[j].Item1;
                    int elemType = allMatches[j].Item2;
                    string cond = text.Substring(elemBegin);
                    cond = cond.Remove(cond.IndexOf("\n"));

                    //The index and condition of the previous if.
                    int prevIfBegin = -1;
                    string prevIfCond = String.Empty;
                    for (int k = j - 1; k >= 0; k--)
                    {
                        if (allMatches[k].Item2 == 0)
                        {
                            prevIfBegin = allMatches[k].Item1;
                            prevIfCond =
                                text.Substring(prevIfBegin);
                            prevIfCond =
                                prevIfCond.Remove(prevIfCond.IndexOf("\n"));
                            break;
                        }
                    }

                    //The index and condition of the previous endif.
                    int prevEndifBegin = -1;
                    string prevEndifCond = String.Empty;
                    for (int k = j - 1; k >= 0; k--)
                    {
                        if (allMatches[k].Item2 == 1)
                        {
                            prevEndifBegin = allMatches[k].Item1;
                            prevEndifCond =
                                text.Substring(prevEndifBegin);
                            prevEndifCond =
                                prevEndifCond.Remove(prevEndifCond.IndexOf("\n"));
                            break;
                        }
                    }

                    //Uses the previous if/endif; whichever is closer.
                    int prevElemBegin = (prevEndifBegin > prevIfBegin)
                        ? prevEndifBegin : prevIfBegin;

                    string prevElemCond = (prevElemBegin == prevEndifBegin)
                        ? prevEndifCond : prevIfCond;

                    //Handles if keywords.
                    if (elemType == 0)
                    {
                        //Adds text between matched keywords. If text was
                        //simply concatenated, it wouldn't preserve order.
                        if (j != 0 && prevIfBegin != -1) //From if to last if.
                        {
                            ParseNode textNode = new ParseNode();
                            textNode.Parent = node;

                            //Determines if the length is negative.
                            int prevElemEnd = prevElemBegin + prevElemCond.Length;
                            if (elemBegin - prevElemEnd < 0)
                            {
                                if (ShowErrors)
                                {
                                    throw new ParserException("parser: " +
                                        "In '" + text.Substring(prevElemBegin) +
                                        "', cannot specify multiple if tokens " +
                                        "on one line.");
                                }

                                continue;
                            }

                            textNode.Text +=
                                text.Substring(prevElemEnd,
                                elemBegin - prevElemEnd);
                            if (!(textNode.Children.Count == 0 &&
                            textNode.Condition.Trim() == String.Empty &&
                            textNode.Text.Trim() == String.Empty))
                            {
                                node.Children.Add(textNode);
                            }
                        }
                        else if (elemBegin > 0) //From start of entry to if.
                        {
                            ParseNode textNode = new ParseNode();
                            textNode.Parent = node;
                            textNode.Text += text.Substring(0, elemBegin);

                            if (!(textNode.Children.Count == 0 &&
                            textNode.Condition.Trim() == String.Empty &&
                            textNode.Text.Trim() == String.Empty))
                            {
                                node.Children.Add(textNode);
                            }
                        }

                        //Creates a child node and sets its parent.
                        ParseNode newChild = new ParseNode();
                        newChild.Parent = node;

                        //Adds the found if statement to the conditions list.
                        newChild.Condition = cond;

                        //Adds the child node and moves node to point to it.
                        if (!(newChild.Children.Count == 0 &&
                            newChild.Condition.Trim() == String.Empty &&
                            newChild.Text.Trim() == String.Empty))
                        {
                            node.Children.Add(newChild);
                        }
                        node = newChild;

                        depth++;
                    }
                    
                    //Handles endif keywords.
                    else if (elemType == 1)
                    {
                        depth--;
                        if (depth < 0)
                        {
                            if (ShowErrors)
                            {
                                throw new ParserException("Parser: an " +
                                "extra endif token was encountered " +
                                "(if/endif #" + (j + 1) + ").");
                            }

                            interpreter.SetEntries(new Dictionary<string, ParseNode>());
                            return;
                        }

                        //Adds text between matched keywords.
                        ParseNode textNode = new ParseNode();
                        textNode.Parent = node;

                        //Determines if the length is negative.
                        int prevElemEnd = prevElemBegin + prevElemCond.Length;
                        if (elemBegin - prevElemEnd < 0)
                        {
                            if (ShowErrors)
                            {
                                throw new ParserException("parser: " +
                                    "In '" + text.Substring(prevElemBegin) +
                                    "', cannot specify multiple endif " +
                                    "tokens on one line.");
                            }

                            continue;
                        }

                        textNode.Text +=
                            text.Substring(prevElemEnd,
                            elemBegin - (prevElemEnd));
                        if (!(textNode.Children.Count == 0 &&
                            textNode.Condition.Trim() == String.Empty &&
                            textNode.Text.Trim() == String.Empty))
                        {
                            node.Children.Add(textNode);
                        }

                        //Points to the node's parent if possible.
                        if (node.Parent != null)
                        {
                            node = node.Parent;
                        }
                        else
                        {
                            if (ShowErrors)
                            {
                                throw new ParserException("Parser: an " +
                                "extra endif token was encountered (endif #" +
                                j + ").");
                            }

                            interpreter.SetEntries(new Dictionary<string, ParseNode>());
                            return;
                        }
                    }
                }

                //Adds all text after last if/endif to the first node.
                if (allMatches.Count > 0)
                {

                    int lastElemBegin = allMatches.Last().Item1;
                    string lastCond = text.Substring(lastElemBegin);
                    int lastCondLength = lastCond.IndexOf("\n");
                    
                    /* Since commands must be on their own lines, if there is
                    * no newline after the last command, it's the last line in
                    * the entry. This means there's nothing after it. So the
                    * last condition executes only if this is false. */
                    if (lastCondLength != -1)
                    {
                        ParseNode textNode = new ParseNode();
                        textNode.Parent = root;
                        textNode.Text += text.Substring(lastElemBegin + lastCondLength);

                        if (!(textNode.Children.Count == 0 &&
                            textNode.Condition.Trim() == String.Empty &&
                            textNode.Text.Trim() == String.Empty))
                        {
                            root.Children.Add(textNode);
                        }
                    }
                }
                //Adds all text to the first node because there were no ifs.
                else
                {
                    root.Text += text;
                }
                
                //Adds the fully constructed entry.
                parsed.Add(entries.Keys.ElementAt(i), root);
            }
            #endregion

            interpreter.SetEntries(parsed, forkToLoad);
        }

        /// <summary>
        /// Returns whether the substring formed by the index and length is in
        /// curly brackets in the given text. Does not support nesting.
        /// </summary>
        private static bool IsOutput(string text, int index, int length)
        {
            string substring = text.Substring(index, length);
            string beforeSubstring = text.Substring(0, index);
            int bracketOpenPos = beforeSubstring.LastIndexOf("{");
            int bracketClosePos = beforeSubstring.LastIndexOf("}");
            
            if ((bracketOpenPos < bracketClosePos) ||
                (bracketOpenPos == -1))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns whether the substring formed by the index and length is on
        /// the same line as an @ symbol. Does not support nesting.
        /// </summary>
        private static bool IsOption(string text, int index, int length)
        {
            int startOfLine = text.Substring(0, index).LastIndexOf("\n");
            if (startOfLine == -1)
            {
                startOfLine = 0;
            }

            int endOfLine = text.Substring(index).IndexOf("\n");
            if (endOfLine == -1)
            {
                endOfLine = text.Length - 1;
            }
            endOfLine += index;

            string line =
                text.Substring(startOfLine, endOfLine - startOfLine);

            if (line.Contains("@"))
            {
                return true;
            }
            
            return false;
        }
        #endregion
    }
}