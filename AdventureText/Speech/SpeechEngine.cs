using System;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace AdventureText.Speech
{
    /// <summary>
    /// A configurable speech recognition and text-to-speech engine.
    /// Speak commands will use text-to-speech and Listen commands use
    /// speech-to-text. For the listener, tags can be provided such that
    /// grammars may be referenced and manipulated by name.
    /// </summary>
    public class SpeechEngine
    {
        #region Members
        /// <summary>
        /// Contains a speech engine to use in speech recognition.
        /// </summary>
        public SpeechRecognitionEngine listenEngine;

        /// <summary>
        /// Contains a speech engine to turn text into speech.
        /// </summary>
        public SpeechSynthesizer speechEngine;

        /// <summary>
        /// Keeps a list of all actions associated with their tokens.
        /// </summary>
        public List<Action> actions;

        /// <summary>
        /// Keeps a list of all tokens associated with their actions.
        /// Multiple strings can be associated with one action.
        /// </summary>
        public List<List<string>> tokens;

        /// <summary>
        /// Fires when all grammars have successfully finished loading.
        /// </summary>
        public event EventHandler ListenReady;

        /// <summary>
        /// True when the engine is actively listening; false otherwise.
        /// </summary>
        public bool IsListening
        {
            private set;
            get;
        }

        /// <summary>
        /// Stores the number of loaded listen texts.
        /// </summary>
        private int _numLoadedListenTexts;

        /// <summary>
        /// Stores the total number of listen texts.
        /// </summary>
        private int _numTotalListenTexts;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the speech recognition engine.
        /// </summary>
        public SpeechEngine()
        {
            //Sets the defaults of the recognition engine.
            listenEngine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
            listenEngine.SetInputToDefaultAudioDevice();
            IsListening = false;

            actions = new List<Action>();
            tokens = new List<List<string>>();

            //Sets a handler for configurable code responses.
            listenEngine.SpeechRecognized += new EventHandler
                <SpeechRecognizedEventArgs>(ListenResponse);

            //Sets a lambda to count when grammars finish loading.
            listenEngine.LoadGrammarCompleted += new EventHandler
                <LoadGrammarCompletedEventArgs>((a, b) =>
                {
                    _numLoadedListenTexts++;

                    if (_numLoadedListenTexts == _numTotalListenTexts)
                    {
                        if (ListenReady != null)
                        {
                            ListenReady(null, null);
                        }
                    }
                });

            //Sets the defaults of the speech engine.
            speechEngine = new SpeechSynthesizer();
            speechEngine.SetOutputToDefaultAudioDevice();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds the specified text as a trigger for recognition. When the
        /// speech is recognized, the given action is executed. Doesn't
        /// load the text automatically.
        /// </summary>
        public void Listen(
            Action code,
            params string[] text)
        {
            List<string> relatedTokens = new List<string>();

            for (int i = 0; i < text.Length; i++)
            {
                //Creates a grammar from text, sets an event name, and loads it.
                Grammar grammar = new Grammar(new GrammarBuilder(text[i]));
                grammar.Name = text[i];
                listenEngine.LoadGrammar(grammar);

                //Adds the token and code to the associated lists.
                relatedTokens.Add(text[i]);

                //Adds to the number of texts being loaded.
                _numTotalListenTexts++;
            }

            tokens.Add(relatedTokens);
            actions.Add(code);
        }

        /// <summary>
        /// Adds the specified text as a trigger for recognition and loads it.
        /// When the speech is recognized, the given action is executed.
        /// </summary>
        /// <param name="tokenName">
        /// The token name helps identify what text was matched for events.
        /// </param>
        /// <param name="text">
        /// The text to listen for.
        /// </param>
        public Grammar Listen(
            string tokenName,
            string text,
            Action code)
        {
            //Creates a grammar from text, sets an event name, and loads it.
            Grammar grammar = new Grammar(new GrammarBuilder(text));
            grammar.Name = tokenName;
            listenEngine.LoadGrammar(grammar);

            //Adds the token and code to the associated lists.
            tokens.Add(new List<string>() { tokenName });
            actions.Add(code);

            //Adds to the number of texts being loaded.
            _numTotalListenTexts++;

            return grammar;
        }

        /// <summary>
        /// Starts recognition (asynchronously).
        /// </summary>
        public void ListenStart()
        {
            if (listenEngine.Grammars.Count > 0)
            {
                listenEngine.RecognizeAsync(RecognizeMode.Multiple);
                IsListening = true;
            }
        }

        /// <summary>
        /// Stops recognition immediately.
        /// </summary>
        public void ListenStop()
        {
            if (listenEngine.Grammars.Count > 0)
            {
                listenEngine.RecognizeAsyncCancel();
                IsListening = false;
            }
        }

        /// <summary>
        /// Executes code registered with a matching speech token.
        /// </summary>
        private void ListenResponse(
            object sender,
            SpeechRecognizedEventArgs e)
        {
            //Gets the name of the matched speech tokens.
            string grammarName = e.Result.Grammar.Name;

            if (grammarName == null)
            {
                grammarName = String.Empty;
            }

            //Finds the matching speech token and invokes its action.
            //Only one action may trigger at once.
            for (int i = 0; i < tokens.Count; i++)
            {
                for (int j = 0; j < tokens[i].Count; j++)
                {
                    if (grammarName.Equals(tokens[i][j]))
                    {
                        if (i < actions.Count)
                        {
                            actions[i]();
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Says the given text asynchronously.
        /// </summary>
        /// <param name="text">
        /// The text to be spoken.
        /// </param>
        public void Speak(string text)
        {
            speechEngine.SpeakAsync(text);
        }

        /// <summary>
        /// Resumes paused text-to-speech.
        /// </summary>
        public void SpeakResume()
        {
            speechEngine.Resume();
        }

        /// <summary>
        /// Pauses text-to-speech.
        /// </summary>
        public void SpeakPause()
        {
            speechEngine.Pause();
        }

        /// <summary>
        /// Aborts any current text-to-speech.
        /// </summary>
        public void SpeakStop()
        {
            speechEngine.SpeakAsyncCancelAll();
        }

        /// <summary>
        /// Unloads all grammars.
        /// </summary>
        public void UnloadAll()
        {
            actions.Clear();
            tokens.Clear();
            _numLoadedListenTexts = 0;
            _numTotalListenTexts = 0;
            listenEngine.UnloadAllGrammars();
        }
        #endregion
    }
}