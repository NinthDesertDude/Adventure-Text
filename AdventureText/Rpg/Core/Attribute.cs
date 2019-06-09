using System;
using System.Collections.Generic;

namespace AdventureText.Rpg.Core
{
    /// <summary>
    /// Represents a value with associated callbacks on set/get.
    /// </summary>
    public class Attribute
    {
        #region Members
        /// <summary>
        /// The value associated with the attribute.
        /// </summary>
        private object value;

        /// <summary>
        /// A list of functions called in-order just before the value changes,
        /// passing in the new value as an argument.
        /// </summary>
        private List<Action<object>> earlyCallbacks;

        /// <summary>
        /// A list of functions called in-order just after the value changes,
        /// passing in the old value as an argument.
        /// </summary>
        private List<Action<object>> lateCallbacks;

        /// <summary>
        /// A list of functions called in-order just before returning the
        /// value. These are used to safely change it.
        /// </summary>
        private List<Action<object>> computeCallbacks;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the value, executing all related callbacks.
        /// </summary>
        public object Value
        {
            get
            {
                for (int i = 0; i < computeCallbacks.Count; i++)
                {
                    computeCallbacks[i].Invoke(value);
                }

                return value;
            }
            set
            {
                for (int i = 0; i < earlyCallbacks.Count; i++)
                {
                    earlyCallbacks[i].Invoke(value);
                }

                object oldValue = this.value;
                this.value = value;

                for (int i = 0; i < lateCallbacks.Count; i++)
                {
                    lateCallbacks[i].Invoke(oldValue);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value without touching callbacks.
        /// </summary>
        public object RawValue
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new attribute with the given value.
        /// </summary>
        public Attribute(object value)
        {
            this.value = value;
            computeCallbacks = new List<Action<object>>();
            earlyCallbacks = new List<Action<object>>();
            lateCallbacks = new List<Action<object>>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a function called when setting a value, before it's set.
        /// </summary>
        public void AddEarlyCallback(Action<object> action)
        {
            earlyCallbacks.Add(action);
        }

        /// <summary>
        /// Removes a specific callback and returns true if it existed.
        /// </summary>
        public bool RemoveEarlyCallback(Action<object> action)
        {
            return earlyCallbacks.Remove(action);
        }

        /// <summary>
        /// Removes all early callbacks.
        /// </summary>
        public void ClearEarlyCallbacks()
        {
            earlyCallbacks.Clear();
        }

        /// <summary>
        /// Returns all early callbacks.
        /// </summary>
        public List<Action<object>> GetEarlyCallbacks()
        {
            return new List<Action<object>>(earlyCallbacks);
        }

        /// <summary>
        /// Adds a function called when setting a value, after it's set.
        /// </summary>
        public void AddLateCallback(Action<object> action)
        {
            lateCallbacks.Add(action);
        }

        /// <summary>
        /// Removes a specific callback and returns true if it existed.
        /// </summary>
        public bool RemoveLateCallback(Action<object> action)
        {
            return lateCallbacks.Remove(action);
        }

        /// <summary>
        /// Removes all late callbacks.
        /// </summary>
        public void ClearLateCallbacks()
        {
            lateCallbacks.Clear();
        }

        /// <summary>
        /// Returns all late callbacks.
        /// </summary>
        public List<Action<object>> GetLateCallbacks()
        {
            return new List<Action<object>>(lateCallbacks);
        }

        /// <summary>
        /// Adds a function called before returning the value.
        /// </summary>
        public void AddComputeCallback(Action<object> action)
        {
            computeCallbacks.Add(action);
        }

        /// <summary>
        /// Removes a specific callback and returns true if it existed.
        /// </summary>
        public bool RemoveComputeCallback(Action<object> action)
        {
            return computeCallbacks.Remove(action);
        }

        /// <summary>
        /// Removes all compute callbacks.
        /// </summary>
        public void ClearComputeCallbacks()
        {
            computeCallbacks.Clear();
        }

        /// <summary>
        /// Returns all compute callbacks.
        /// </summary>
        public List<Action<object>> GetComputeCallbacks()
        {
            return new List<Action<object>>(computeCallbacks);
        }
        #endregion
    }
}
