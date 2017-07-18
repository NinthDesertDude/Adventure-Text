#if DEBUG
#define SHOWERRORS //Generate exceptions for errors?
#define PRINTERRORS //Catch exceptions to display on screen?
#endif

using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace AdventureText
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Methods
        /// <summary>
        /// Initializes the console manually.
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            #if SHOWERRORS //Errors are generated.
                Parsing.Parser.ShowErrors = true;
                Parsing.Interpreter.ShowErrors = true;
            #endif

            //Sets up the console with an interpreter to run the game.
            Console cons = new Console(new ConsoleGui());
            
            //Sets default console options.
            cons.PrefOptFontSize = 24;
            cons.PrefOptFontFamily = new FontFamily("Andy, Helvetica, Sans Serif");
            
            #if PRINTERRORS
                Parsing.Interpreter.PrintErrors = true;
            #endif

            //Gets and handles command line arguments.
            //Argument 1: The filename to load, if possible.
            //Argument 2: The fork to load, if possible.
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 2)
            {
                args[2] = Regex.Replace(args[2], "^@", "").Trim().ToLower();
            }
            string defaultFile = (args.Length > 1) ? args[1] : "game.txt";
            string defaultFork = (args.Length > 2) ? args[2] : String.Empty;

            //Hooks an interpreter to the console and parses a file.
            #if PRINTERRORS //Errors are printed.
                Utils.TryLoadFile(cons, defaultFile, defaultFork);
            #else //Errors are silent.
                Utils.LoadFile(cons, defaultFile, defaultFork);
            #endif
        }
        #endregion
    }
}