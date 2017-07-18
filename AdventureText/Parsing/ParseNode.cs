using System;
using System.Collections.Generic;

namespace AdventureText.Parsing
{
    /// <summary>
    /// Represents a node in a hierarchy of nodes.
    /// </summary>
    class ParseNode
    {
        #region Properties
        /// <summary>
        /// Contains a list of conditions to be met for text to be considered.
        /// </summary>
        public string Condition
        {
            get;
            set;
        }

        /// <summary>
        /// Contains text to be processed only if the conditions are met.
        /// </summary>
        public string Text
        {
            get;
            set;
        }

        /// <summary>
        /// Contains a reference to the parent node, if any.
        /// </summary>
        public ParseNode Parent
        {
            get;
            set;
        }

        /// <summary>
        /// Contains references to all child nodes, if any.
        /// </summary>
        public List<ParseNode> Children
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a node representing a condition. Intended for parsing.
        /// </summary>
        public ParseNode()
        {
            Condition = String.Empty;
            Text = String.Empty;
            Parent = null;
            Children = new List<ParseNode>();
        }
        #endregion
    }
}
