namespace AdventureText.MathParsing
{
    /// <summary>
    /// An immutable identifier token, such as a constant or variable.
    /// </summary>
    public class LiteralId : Token
    {
        #region Properties
        /// <summary>
        /// The associated numeric value, if set.
        /// </summary>
        public object Value { get; protected set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an identifier parsing token with an unknown value.
        /// </summary>
        /// <param name="name">
        /// The unique name for the identifier.
        /// </param>
        public LiteralId(string name)
        {
            StrForm = name;
        }

        /// <summary>
        /// Creates an identifier parsing token.
        /// </summary>
        /// <param name="name">
        /// The unique name for the identifier.
        /// </param>
        /// <param name="value">
        /// The number encapsulated in the token.
        /// </param>
        public LiteralId(string name, decimal value)
        {
            StrForm = name;
            Value = value;
        }

        /// <summary>
        /// Creates an identifier parsing token.
        /// </summary>
        /// <param name="name">
        /// The unique name for the identifier.
        /// </param>
        /// <param name="value">
        /// The bool encapsulated in the token.
        /// </param>
        public LiteralId(string name, bool value)
        {
            StrForm = name;
            Value = value;
        }

        /// <summary>
        /// Returns true if all properties of each token are the same.
        /// </summary>
        /// <param name="obj">
        /// The token to compare against for equality.
        /// </param>
        public bool Equals(LiteralId obj)
        {
            return (StrForm == obj.StrForm &&
                Value == obj.Value);
        }
        #endregion
    }
}