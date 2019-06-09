using System.Collections.Generic;

namespace AdventureText.Rpg.Core
{
    /// <summary>
    /// Represents a collection of attributes.
    /// </summary>
    public class AttributeCollection
    {
        #region Members
        /// <summary>
        /// A list of all attributes defined in the module.
        /// </summary>
        Dictionary<string, Attribute> attributes;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new collection of attributes.
        /// </summary>
        public AttributeCollection()
        {
            attributes = new Dictionary<string, Attribute>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds the given attribute if it doesn't exist by name, returning
        /// true or otherwise false.
        /// </summary>
        public bool AddAttribute(string name, Attribute attr)
        {
            if (attributes.ContainsKey(name))
            {
                return false;
            }

            attributes.Add(name, attr);
            return true;
        }

        /// <summary>
        /// Removes the given attribute by name.
        /// </summary>
        public bool RemoveAttribute(string name)
        {
            return attributes.Remove(name);
        }

        /// <summary>
        /// Removes all attributes.
        /// </summary>
        public void ClearAttributes()
        {
            attributes.Clear();
        }

        /// <summary>
        /// Sets the given attribute if it exists, returning true otherwise
        /// false.
        /// </summary>
        public bool SetAttribute(string name, Attribute attr)
        {
            if (attributes.ContainsKey(name))
            {
                attributes[name] = attr;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a new dictionary containing all attributes.
        /// </summary>
        public Dictionary<string, Attribute> GetAttributes()
        {
            return new Dictionary<string, Attribute>(attributes);
        }

        /// <summary>
        /// Access attributes directly by name, e.g. myModule["myAttr"].
        /// </summary>
        public Attribute this[string key]
        {
            get
            {
                return attributes[key];
            }
            set
            {
                attributes[key] = value;
            }
        }
        #endregion
    }
}