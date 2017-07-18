using System;

namespace AdventureText.Parsing
{
    /// <summary>
    /// Represents any error encountered while interpreting parsed data.
    /// </summary>
    class InterpreterException : Exception
    {
        #region Constructors
        public InterpreterException()
            : base()
        {
        }

        public InterpreterException(string message)
            : base(message)
        {
        }

        public InterpreterException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public InterpreterException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
        #endregion
    }
}
