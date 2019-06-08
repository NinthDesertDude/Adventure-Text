using System;
using System.Windows.Documents;

namespace AdventureText
{
    /// <summary>
    /// Displays prior text input/output during a game session.
    /// </summary>
    class Log
    {
        #region Fields
        /// <summary>
        /// Stores the corresponding console in order to mimic the style.
        /// </summary>
        private Console console;

        /// <summary>
        /// The encapsulated gui.
        /// </summary>
        private LogGui gui;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new window for viewing prior fork content.
        /// </summary>
        public Log(Console console)
        {
            gui = new LogGui();
            this.console = console;
            gui.Background = console.PrefBackColor;
            gui.FontFamily = console.PrefOutFontFamily;
            gui.FontSize = console.PrefOutFontSize;
            gui.FontWeight = console.PrefOutFontWeight;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Removes all logs.
        /// </summary>
        public void Clear()
        {
            gui.GuiText.Text = "";
        }

        /// <summary>
        /// Logs input from the user with the same color as options.
        /// </summary>
        public void LogInput(string text)
        {
            //Provides clean space in the log.
            if (gui.GuiText.Inlines.Count > 0)
            {
                text = Environment.NewLine + text + Environment.NewLine;
            }

            Run log = new Run(text);
            log.FontFamily = console.PrefOptFontFamily;
            log.FontSize = console.PrefOptFontSize;
            log.FontWeight = console.PrefOptFontWeight;
            log.Foreground = console.PrefOptColor;
            gui.GuiText.Inlines.Add(log);
        }

        /// <summary>
        /// Logs output from the game with the same color as the console.
        /// </summary>
        public void LogOutput(string text)
        {
            Run log = new Run(text);
            log.FontFamily = console.PrefOutFontFamily;
            log.FontSize = console.PrefOutFontSize;
            log.FontWeight = console.PrefOutFontWeight;
            log.Foreground = console.PrefOutColor;
            gui.GuiText.Inlines.Add(log);
        }

        /// <summary>
        /// Displays the log.
        /// </summary>
        public void Show()
        {
            gui.Show();
        }
        #endregion
    }
}
