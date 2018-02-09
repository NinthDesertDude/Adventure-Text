using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdventureText.Speech;
using System.Collections.Generic;

namespace AdventureText
{
    /// <summary>
    /// Wraps the console gui and provides intended functionality.
    /// </summary>
    class Console
    {
        #region Fields
        private ConsoleGui _gui; //The underlying gui.

        private FontFamily _prefOutFontFamily; //Default font family.

        private double _prefOutFontSize; //Default font size.

        private FontWeight _prefOutFontWeight; //Default font weight.

        private Brush _prefOutColor; //The color of the text.

        private Brush _prefBackColor; //The color of the console.

        private FontFamily _prefOptFontFamily; //Default options font family.

        private double _prefOptFontSize; //Default options font size.

        private FontWeight _prefOptFontWeight; //Default options font weight.

        private Brush _prefOptColor; //The un-hovered options color.

        private Brush _prefOptHighlightColor; //The hovered options color.

        private SpeechEngine _speechEngine; //For general speech support.

        private bool _logEnabled; //Whether input and output is logged or not.

        private List<Tuple<LogTypes, string>> _log; //A log to track content.
        #endregion

        #region Properties
        /// <summary>
        /// Sets/gets the font family for the console.
        /// </summary>
        public FontFamily PrefOutFontFamily
        {
            get
            {
                return _prefOutFontFamily;
            }
            set
            {
                _prefOutFontFamily = value;
                _gui.FontFamily = _prefOutFontFamily;
            }
        }
        
        /// <summary>
        /// Sets/gets the font family for options in the console.
        /// </summary>
        public FontFamily PrefOptFontFamily
        {
            get
            {
                return _prefOptFontFamily;
            }
            set
            {
                _prefOptFontFamily = value;
            }
        }
        
        /// <summary>
        /// Sets/gets the font size for the console.
        /// </summary>
        public double PrefOutFontSize
        {
            get
            {
                return _prefOutFontSize;
            }
            set
            {
                _prefOutFontSize = value;
                _gui.FontSize = _prefOutFontSize;
            }
        }
        
        /// <summary>
        /// Sets/gets the font weight for the console.
        /// </summary>
        public FontWeight PrefOutFontWeight
        {
            get
            {
                return _prefOutFontWeight;
            }
            set
            {
                _prefOutFontWeight = value;
                _gui.FontWeight = _prefOutFontWeight;
            }
        }
        
        /// <summary>
        /// Sets/gets the font color for the console.
        /// </summary>
        public Brush PrefOutColor
        {
            get
            {
                return _prefOutColor;
            }
            set
            {
                _prefOutColor = value;
                _gui.Foreground = _prefOutColor;
            }
        }
        
        /// <summary>
        /// Sets/gets the console's background color.
        /// </summary>
        public Brush PrefBackColor
        {
            get
            {
                return _prefBackColor;
            }
            set
            {
                _prefBackColor = value;
                _gui.Background = _prefBackColor;
            }
        }
        
        /// <summary>
        /// Sets/gets the default color of options.
        /// </summary>
        public Brush PrefOptColor
        {
            get
            {
                return _prefOptColor;
            }
            set
            {
                _prefOptColor = value;
            }
        }
        
        /// <summary>
        /// Sets/gets the default color of highlighted options.
        /// </summary>
        public Brush PrefOptHighlightColor
        {
            get
            {
                return _prefOptHighlightColor;
            }
            set
            {
                _prefOptHighlightColor = value;
            }
        }
        
        /// <summary>
        /// Sets/gets the font size for options in the console.
        /// </summary>
        public double PrefOptFontSize
        {
            get
            {
                return _prefOptFontSize;
            }
            set
            {
                _prefOptFontSize = value;
            }
        }
        
        /// <summary>
        /// Sets/gets the font weight for options in the console.
        /// </summary>
        public FontWeight PrefOptFontWeight
        {
            get
            {
                return _prefOptFontWeight;
            }
            set
            {
                _prefOptFontWeight = value;
            }
        }
        
        /// <summary>
        /// Provides speech and text conversion functionality.
        /// </summary>
        public SpeechEngine SpeechEngine
        {
            set
            {
                _speechEngine = value;
            }
            get
            {
                return _speechEngine;
            }
        }

        /// <summary>
        /// Whether input and output is logged or not.
        /// </summary>
        public bool LogEnabled
        {
            get
            {
                return _logEnabled;
            }
            set
            {
                _logEnabled = value;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates and shows a new console with the given gui.
        /// </summary>
        /// <param name="gui">
        /// The gui to use.
        /// </param>
        public Console(ConsoleGui gui)
        {
            //Sets default values.
            _gui = gui;
            _speechEngine = new SpeechEngine();
            _log = new List<Tuple<LogTypes, string>>();

            //Sets default console preferences.
            ResetPreferences();

            //Subscribe to events.
            _gui.GuiInput.KeyDown += Submit;
            _gui.Closed += Dispose;

            //The private keydown event triggers a public one to subscribe to.
            _gui.GuiWindow.KeyDown += new KeyEventHandler((a, b) =>
                { OnKeyDown?.Invoke(a, b); });

            _gui.Show();
        }
        #endregion

        #region Delegates and Events
        /// <summary>
        /// Used to handle textbox submissions on pressing enter.
        /// </summary>
        /// <param name="sender">
        /// The object raising the event.
        /// </param>
        /// <param name="text">
        /// The text typed by the user.
        /// </param>
        public delegate void OnSubmitHandler(object sender, string text);

        /// <summary>
        /// Fires when the user presses enter with focus in the textbox.
        /// </summary>
        public event OnSubmitHandler OnSubmit;

        /// <summary>
        /// Fires when the user presses a key.
        /// </summary>
        public event KeyEventHandler OnKeyDown;
        #endregion

        #region Methods
        /// <summary>
        /// Raises the OnSubmit event when the user presses enter. Clears the
        /// textbox. Note: Submit subscribers can be cleared all at once.
        /// </summary>
        private void Submit(object sender, KeyEventArgs e)
        {
            //Handles textbox submission.
            if (_gui.GuiInput.IsFocused && Keyboard.IsKeyDown(Key.Enter))
            {
                OnSubmit?.Invoke(this, _gui.GuiInput.Text);
                _gui.GuiInput.Clear();
            }
        }

        /// <summary>
        /// Adds an image to be displayed from a relative path located at the
        /// project root. All images are displayed at the top of the screen.
        /// </summary>
        /// <param name="path">
        /// A .png file and relative path, without the
        /// filename extension. Example: Resources/MyImage
        /// </param>
        public void AddImage(string path, bool isRelativePath = true)
        {
            if (isRelativePath)
            {
                AddImage(new Uri(Path.Combine(
                    Environment.CurrentDirectory,
                    path)));
            }
            else
            {
                AddImage(new Uri(path));
            }
        }

        /// <summary>
        /// Adds an image to be displayed. All images are displayed at the top
        /// of the screen.
        /// </summary>
        /// <param name="img">
        /// The image to be displayed.
        /// </param>
        public void AddImage(Image img)
        {
            _gui.GuiImages.Children.Add(img);
        }

        /// <summary>
        /// Adds an image from an URI to be displayed. All images are
        /// displayed at the top of the screen. There is no error checking.
        /// </summary>
        /// <param name="img">
        /// The image to be displayed.
        /// </param>
        public void AddImage(Uri uri)
        {
            //Creates an image and instantiates it from the given uri.
            Image img = new Image();
            BitmapImage bitmapImg = new BitmapImage();

            //Attempts to load the image from the uri.
            bitmapImg.BeginInit();
            bitmapImg.UriSource = uri;
            bitmapImg.EndInit();

            img.Source = bitmapImg;
            img.Stretch = Stretch.None;

            //Adds the new image.
            _gui.GuiImages.Children.Add(img);
        }

        /// <summary>
        /// Adds to the options.
        /// </summary>
        /// <param name="text">
        /// The text to be added to the options.
        /// </param>
        /// <param name="clickAction">
        /// The action to perform when clicked.
        /// </param>
        public void AddOption(string text, Action clickAction)
        {
            //Ensures there is a newline at the end so all options
            //are vertically separated.
            if (!text.EndsWith("\n"))
            {
                text += "\n";
            }

            //Generates a run.
            Run generatedRun = new Run(text);
            generatedRun.Foreground = PrefOptColor;
            generatedRun.FontFamily = PrefOptFontFamily;
            generatedRun.FontSize = PrefOptFontSize;
            generatedRun.FontWeight = PrefOptFontWeight;

            //Captures colors to local variables to keep them constant.
            Brush optHgltColor = PrefOptHighlightColor;
            Brush optColor = PrefOptColor;

            //Changes color in response to mouse.
            generatedRun.MouseEnter += (a, b) =>
            {
                generatedRun.Foreground = optHgltColor;
            };
            generatedRun.MouseLeave += (a, b) =>
            {
                generatedRun.Foreground = optColor;
            };

            //Responds to clicks.
            generatedRun.MouseLeftButtonDown += (a, b) =>
            {
                if (LogEnabled)
                {
                    _log.Add(new Tuple<LogTypes, string>(LogTypes.Input, text));
                }

                clickAction.Invoke();
            };

            //Adds to options.
            _gui.GuiOptions.Inlines.Add(generatedRun);
        }

        /// <summary>
        /// Concatenates the given text to the options (without a newline).
        /// Note that with InlineUIContainer it's possible to add elements,
        /// though this practice is not recommended.
        /// </summary>
        /// <param name="text">
        /// The text to be added to the options.
        /// </param>
        /// <param name="clickAction">
        /// The action to perform when clicked.
        /// </param>
        public void AddOption(Run run, Action clickAction)
        {
            //Captures colors to local variables to keep them constant.
            Brush optHgltColor = PrefOptHighlightColor;
            Brush optColor = PrefOptColor;

            //Changes color in response to mouse.
            run.MouseEnter += (a, b) =>
            {
                run.Foreground = optHgltColor;
            };
            run.MouseLeave += (a, b) =>
            {
                run.Foreground = optColor;
            };

            //Responds to clicks.
            run.MouseLeftButtonDown += (a, b) =>
            {
                if (LogEnabled)
                {
                    _log.Add(new Tuple<LogTypes, string>(LogTypes.Input, run.Text));
                }

                clickAction.Invoke();
            };

            _gui.GuiOptions.Inlines.Add(run);
        }

        /// <summary>
        /// Concatenates the given text to the output (without a newline).
        /// </summary>
        /// <param name="text">
        /// The text to be added to the output.
        /// </param>
        public void AddText(string text)
        {
            Run generatedRun = new Run(text);
            generatedRun.Foreground = PrefOutColor;
            generatedRun.FontFamily = PrefOutFontFamily;
            generatedRun.FontSize = PrefOutFontSize;
            generatedRun.FontWeight = PrefOutFontWeight;

            _gui.GuiOutput.Inlines.Add(generatedRun);

            if (LogEnabled)
            {
                _log.Add(new Tuple<LogTypes, string>(LogTypes.Output, text));
            }
        }

        /// <summary>
        /// Concatenates the given text to the output (without a newline).
        /// Note that with InlineUIContainer it's possible to add elements,
        /// though this practice is not recommended.
        /// </summary>
        /// <param name="text">
        /// The text to be added to the output.
        /// </param>
        public void AddText(Run run)
        {
            _gui.GuiOutput.Inlines.Add(run);

            if (LogEnabled)
            {
                _log.Add(new Tuple<LogTypes, string>(LogTypes.Output, run.Text));
            }
        }

        /// <summary>
        /// Clears the screen of all images, output, and input. Does not
        /// reset preferences.
        /// </summary>
        public void Clear()
        {
            _gui.GuiInput.Clear();
            _gui.GuiOutput.Inlines.Clear();
            _gui.GuiImages.Children.Clear();
            _gui.GuiOptions.Inlines.Clear();

            //Resets scrolling.
            _gui.GuiScroll.ScrollToTop();
        }

        /// <summary>
        /// Disposes unhandled memory before closing the program.
        /// </summary>
        public void Dispose(object sender, EventArgs e)
        {
            _speechEngine.listenEngine.Dispose();
            _speechEngine.speechEngine.Dispose();
        }

        /// <summary>
        /// Gets whether the textbox is enabled.
        /// </summary>
        public bool GetInputEnabled()
        {
            return _gui.GuiInput.IsEnabled;
        }

        /// <summary>
        /// Returns the current number of images.
        /// </summary>
        /// <returns>
        /// The number of images added.
        /// </returns>
        public int GetImagesAmount()
        {
            return _gui.GuiImages.Children.Count;
        }

        /// <summary>
        /// Returns the current number of options strings.
        /// </summary>
        /// <returns>
        /// The number of individual strings of text added.
        /// </returns>
        public int GetOptionsAmount()
        {
            return _gui.GuiOptions.Inlines.Count;
        }

        /// <summary>
        /// Returns the current number of text strings.
        /// </summary>
        /// <returns>
        /// The number of individual strings of text added.
        /// </returns>
        public int GetTextAmount()
        {
            return _gui.GuiOutput.Inlines.Count;
        }

        /// <summary>
        /// Resets all console preferences to their default values.
        /// </summary>
        public void ResetPreferences()
        {
            PrefBackColor = Brushes.Black;

            PrefOutColor = Brushes.White;
            PrefOutFontFamily = new FontFamily("Arial,Helvetica,Sans Serif");
            PrefOutFontSize = 16;
            PrefOutFontWeight = FontWeights.Normal;

            PrefOptColor = Brushes.Red;
            PrefOptHighlightColor = Brushes.White;
            PrefOptFontFamily = new FontFamily("Arial,Helvetica,Sans Serif");
            PrefOptFontSize = 16;
            PrefOptFontWeight = FontWeights.Normal;

            LogEnabled = true;
        }

        /// <summary>
        /// Sets the height of the console.
        /// </summary>
        /// <param name="height">
        /// The new height of the console.
        /// </param>
        public void SetHeight(int height)
        {
            if (height <= 0)
            {
                return;
            }

            _gui.Height = height;
        }

        /// <summary>
        /// Sets whether the textbox is enabled.
        /// </summary>
        public void SetInputEnabled(bool value)
        {
            _gui.GuiInput.IsEnabled = value;

            //Ensures the input is hidden when disabled.
            if (_gui.GuiInput.IsEnabled)
            {
                _gui.GuiInput.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                _gui.GuiInput.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Sets the console's window caption.
        /// </summary>
        /// <param name="title">
        /// The new title caption to use.
        /// </param>
        public void SetTitle(string title)
        {
            _gui.Title = title;
        }

        /// <summary>
        /// Sets the width of the console.
        /// </summary>
        /// <param name="width">
        /// The new width of the console.
        /// </param>
        public void SetWidth(int width)
        {
            if (width <= 0)
            {
                return;
            }

            _gui.Width = width;
        }

        /// <summary>
        /// Returns the log.
        /// </summary>
        public List<Tuple<LogTypes, string>> GetLog()
        {
            return _log;
        }

        /// <summary>
        /// Clears the log.
        /// </summary>
        public void ClearLog()
        {
            _log.Clear();
        }
        #endregion
    }
}