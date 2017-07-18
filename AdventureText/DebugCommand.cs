using System;
using System.Windows.Documents;

namespace AdventureText
{
    /// <summary>
    /// Encapsulates the debug command gui to provide functionality.
    /// </summary>
    class DebugCommand
    {
        #region Fields
        /// <summary>
        /// The encapsulated gui.
        /// </summary>
        private DebugCommandGui gui;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new debug command window.
        /// </summary>
        public DebugCommand()
        {
            //Variable initialization.
            gui = new DebugCommandGui();

            //Event bindings.
            gui.KeyDown += Gui_CloseWindow;
            gui.GuiInput.KeyDown += GuiInput_SubmitText;
            gui.GuiInput.Focus();
            gui.GuiNavigatePanel.GotFocus += new System.Windows.RoutedEventHandler((a, b) =>
                {
                    OnLoadForks?.Invoke();
                });
            gui.GuiVariablesPanel.GotFocus += new System.Windows.RoutedEventHandler((a, b) =>
            {
                OnLoadVariables?.Invoke();
            });
        }
        #endregion

        #region Delegates and Events
        /// <summary>
        /// Triggers when the user submits text in the execute tab.
        /// </summary>
        public event Action<string> OnExecuteQuery;

        /// <summary>
        /// Triggers when the user clicks the fork tab.
        /// </summary>
        public event Action OnLoadForks;

        /// <summary>
        /// Triggers when the user clicks the variables tab.
        /// </summary>
        public event Action OnLoadVariables;
        #endregion

        #region Methods
        /// <summary>
        /// Closes the window when escape is pressed.
        /// </summary>
        private void Gui_CloseWindow(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //If escape is pressed.
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                gui.Close();
            }
        }

        /// <summary>
        /// Handles the submission action for the input textbox.
        /// </summary>
        private void GuiInput_SubmitText(object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            //If enter is pressed.
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SubmitExecText();
            }
        }

        /// <summary>
        /// Shows as a dialog that pauses the text adventure engine until done.
        /// </summary>
        public void ShowDialog()
        {
            //Populates the lists.
            OnLoadForks?.Invoke();
            OnLoadVariables?.Invoke();

            gui.ShowDialog();
        }

        /// <summary>
        /// Submits and clears the executable user text.
        /// </summary>
        public void SubmitExecText()
        {
            OnExecuteQuery?.Invoke(gui.GuiInput.Text);
            ClearExecText();
        }

        /// <summary>
        /// Clears the executable user text.
        /// </summary>
        public void ClearExecText()
        {
            gui.GuiInput.Clear();
        }

        /// <summary>
        /// Sets the execute info.
        /// </summary>
        public void SetExecInfo(string text)
        {
            gui.GuiResponse.Text = text;
        }

        /// <summary>
        /// Clears the execute info.
        /// </summary>
        public void ClearExecInfo(string text)
        {
            gui.GuiResponse.Text = String.Empty;
        }

        /// <summary>
        /// Adds to the fork list.
        /// </summary>
        public void AddToForks(Run forkOption)
        {
            gui.GuiNavigateForks.Inlines.Add(forkOption);
        }

        /// <summary>
        /// Clears the fork list.
        /// </summary>
        public void ClearForks()
        {
            gui.GuiNavigateForks.Inlines.Clear();
        }

        /// <summary>
        /// Adds to the variable list.
        /// </summary>
        public void AddToVariables(Run variable)
        {
            gui.GuiVariables.Inlines.Add(variable);
        }

        /// <summary>
        /// Clears the variable list.
        /// </summary>
        public void ClearVariables()
        {
            gui.GuiVariables.Inlines.Clear();
        }

        /// <summary>
        /// Refreshes the fork and variable lists.
        /// </summary>
        public void RefreshLists()
        {
            OnLoadForks();
            OnLoadVariables();
        }
        #endregion
    }
}
