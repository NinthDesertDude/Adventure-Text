using System;

namespace AdventureText.Parsing
{
    /// <summary>
    /// Represents any error encountered while parsing text.
    /// </summary>
    class ParserException : Exception
    {
        #region Constructors
        public ParserException()
            : base()
        {
        }

        public ParserException(string message)
            : base(message)
        {
        }

        public ParserException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public ParserException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
        #endregion
    }
}
