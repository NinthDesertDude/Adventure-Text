using System;
using System.Collections.Generic;

namespace AdventureText.Rpg.Core
{
    /// <summary>
    /// Encapsulates a character with functions to customize combat logic.
    /// </summary>
    public class CombatCharacter
    {
        #region Members
        /// <summary>
        /// All combat characters with the same team id are on the same team.
        /// </summary>
        public int teamId;

        /// <summary>
        /// A list of executable actions identified by name that get all
        /// team information as input.
        /// </summary>
        private Dictionary<string, Action<List<List<CombatCharacter>>>> combatActions;
        #endregion

        #region Properties
        /// <summary>
        /// The character represented in combat.
        /// </summary>
        public Character Character
        {
            get;
            private set;
        }

        /// <summary>
        /// Integer. The combat speed determined turn order. Characters with
        /// higher speeds go first.
        /// The recommended approach is to set a compute callback that sets
        /// the speed depending on factors like equipped item speed, spell
        /// casting speed, etc.
        /// </summary>
        public Attribute CombatSpeed
        {
            get;
            set;
        }

        /// <summary>
        /// Boolean. When true, the character will be removed from combat.
        /// The recommended approach is to set a compute callback that sets
        /// to true or false depending on factors like character health.
        /// </summary>
        public Attribute RemoveFromCombat
        {
            get;
            set;
        }

        /// <summary>
        /// The action to perform for this character's combat step. A list of
        /// all combat characters is passed as an argument, in the order they
        /// take their turns.
        /// </summary>
        public Action<List<CombatCharacter>> CombatAction
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Wraps a character with combat logic.
        /// </summary>
        public CombatCharacter(Character character)
        {
            combatActions = new Dictionary<string, Action<List<List<CombatCharacter>>>>();
            Character = character;
            CombatSpeed = new Attribute(0);
            CombatAction = null;
            RemoveFromCombat = new Attribute(false);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public CombatCharacter(CombatCharacter other)
        {
            combatActions = other.combatActions;
            Character = other.Character;
            CombatSpeed = other.CombatSpeed;
            CombatAction = other.CombatAction;
            RemoveFromCombat = other.RemoveFromCombat;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds the given action if the name doesn't exist yet, or
        /// overwrites the existing action.
        /// </summary>
        public void SetAction(string name, Action<List<List<CombatCharacter>>> action)
        {
            if (combatActions.ContainsKey(name))
            {
                combatActions[name] = action;
            }
            else
            {
                combatActions.Add(name, action);
            }
        }

        /// <summary>
        /// Removes the given action by name, returning true if it existed,
        /// else false.
        /// </summary>
        public bool RemoveAction(string name)
        {
            return combatActions.Remove(name);
        }

        /// <summary>
        /// Removes all combat actions.
        /// </summary>
        public void ClearActions()
        {
            combatActions.Clear();
        }
        #endregion
    }
}