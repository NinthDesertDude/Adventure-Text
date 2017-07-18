using AdventureText.Parsing;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Media;

namespace AdventureText
{
    /// <summary>
    /// Contains globally used functionality.
    /// </summary>
    class Utils
    {
        #region Static Members
        /// <summary>
        /// Stores the loaded file's url. Empty by default.
        /// </summary>
        private static string _url = String.Empty;
        #endregion

        #region Static Properties
        /// <summary>
        /// Stores the loaded file's directory. Empty by default.
        /// </summary>
        public static string Url
        {
            get
            {
                return _url;
            }
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Creates a user prompt to open a game file saved as plaintext. If a
        /// file called 'game.txt' exists in the current directory, it's used
        /// automatically. Otherwise, a different file is loaded.
        /// </summary>
        /// <param name="cons">
        /// The console to use when loading the file.
        /// </param>
        /// <param name="defaultFile">
        /// The default file to try to load before presenting an open-file
        /// dialog to the user.
        /// </param>
        public static void LoadFile(Console cons,
            string defaultFile, string forkToLoad)
        {
            string url = defaultFile;

            //Loads the default file automatically if possible.
            if (File.Exists(defaultFile))
            {
                _url = Path.GetDirectoryName(defaultFile);
                Parser.LoadFile(url, new Interpreter(cons), forkToLoad);
                return;
            }

            //Otherwise, opens a separate dialog.
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.CheckPathExists = true;
            dlg.Filter = "game files|*.txt";
            dlg.Title = "Open game";

            if (dlg.ShowDialog() == true)
            {
                url = dlg.FileName;
                _url = Path.GetDirectoryName(url);
                Parser.LoadFile(url, new Interpreter(cons), forkToLoad);
            }

            else
            {
                cons.Clear();
                cons.AddOption("Click to load a file.", new Action(() =>
                {
                    LoadFile(cons, String.Empty, String.Empty);
                }));

                return;
            }
        }

        /// <summary>
        /// Attempts to load a file. If an error occurs, prints the error to
        /// the screen and generates an option to load a different file.
        /// </summary>
        /// <param name="cons">
        /// The console to use when loading the file.
        /// </param>
        /// <param name="defaultFile">
        /// The default file to try to load before presenting an open-file
        /// dialog to the user.
        /// </param>
        public static void TryLoadFile(Console cons,
            string defaultFile, string forkToLoad)
        {
            bool errorOccurred = false;

            try
            {
                LoadFile(cons, defaultFile, forkToLoad);
            }

            //Prints errors from parser/interpreter to the screen.
            catch (Exception ex)
            {
                cons.Clear();
                cons.PrefOutColor = Brushes.Yellow;

                if (ex is ParserException ||
                    ex is InterpreterException)
                {
                    cons.AddText("\nA loading error occurred. " + ex.Message);
                }
                else
                {
                    cons.AddText("\nAn error occurred. " + ex.Message);
                }

                cons.PrefOutColor = Brushes.White;

                errorOccurred = true;
            }

            //Outside the catch block to avoid throwing exceptions in it.
            if (errorOccurred)
            {
                cons.AddOption("Click to load a file.", new Action(() =>
                {
                    TryLoadFile(cons, String.Empty, String.Empty);
                }));
            }
        }
        #endregion
    }
}
