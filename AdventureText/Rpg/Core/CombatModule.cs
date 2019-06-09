using System;
using System.Collections.Generic;
using System.Linq;

namespace AdventureText.Rpg.Core
{
    /// <summary>
    /// Handles turn-based combat logic, providing hooks for combat actions
    /// and bindings for attributes involved in combat, with separate hooks
    /// for automatic computation of those attributes.
    /// </summary>
    class CombatModule
    {
        #region Members
        /// <summary>
        /// Stores which groups of characters fight as allies.
        /// </summary>
        private List<List<CombatCharacter>> teams;

        /// <summary>
        /// A queue of characters to be removed when combat logic finishes.
        /// </summary>
        private List<CombatCharacter> charactersToRemove;

        /// <summary>
        /// A queue of characters to be added when combat logic finishes.
        /// </summary>
        private List<CombatCharacter> charactersToAdd;

        /// <summary>
        /// Stores a reference to the current character in combat. If combat
        /// isn't in session, this is null.
        /// </summary>
        private CombatCharacter currentCombatCharacter;

        /// <summary>
        /// True if combat is being calculated.
        /// </summary>
        private bool isInCombat;

        /// <summary>
        /// True if combat should stop as soon as possible.
        /// </summary>
        private bool doStopCombat;
        #endregion

        #region Properties
        /// <summary>
        /// When true, instead of stopping combat when there's only one team
        /// left, each remaining combatant's team ID will be set to a unique
        /// value starting at 0 and counting up. Then combat will continue
        /// until there is only one character left or it's manually stopped.
        /// </summary>
        public bool TeamFightsWhenDone
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a combat module to handle turn-based combat logic.
        /// </summary>
        /// <param name="chars">
        /// A list of all characters to include in combat.
        /// </param>
        public CombatModule(List<CombatCharacter> chars)
        {
            charactersToRemove = new List<CombatCharacter>();
            charactersToAdd = new List<CombatCharacter>();
            currentCombatCharacter = null;
            isInCombat = false;
            doStopCombat = false;
            TeamFightsWhenDone = false;

            //Groups all characters into teams to load them.
            teams = new List<List<CombatCharacter>>();
            var list = chars.GroupBy((o) => { return o.teamId; });

            for (int i = 0; i < list.Count(); i++)
            {
                teams.Add(list.ElementAt(i).ToList());
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds the given character to combat if combat is not currently
        /// being computed.
        /// </summary>
        /// <param name="character"></param>
        public void AddToCombat(CombatCharacter character)
        {
            //Adds characters to the appropriate team immediately.
            if (!isInCombat)
            {
                for (int i = 0; i < teams.Count; i++)
                {
                    if (teams[i].Count > 0 &&
                        teams[i][0].teamId == character.teamId)
                    {
                        teams[i].Add(character);
                        return;
                    }

                    teams.Add(new List<CombatCharacter>() { character });
                }
            }

            //Queues them to be added at next possible opportunity.
            else
            {
                charactersToAdd.Add(character);
            }
        }

        /// <summary>
        /// Removes the given character from combat if combat is not currently
        /// being computed. Returns true if character was found, else false.
        /// </summary>
        public bool RemoveFromCombat(CombatCharacter character)
        {
            //Removes a character from the appropriate team immediately.
            if (!isInCombat)
            {
                for (int i = 0; i < teams.Count; i++)
                {
                    if (teams[i].Remove(character))
                    {
                        return true;
                    }
                }

                return false;
            }

            //Queues them to be added at next possible opportunity.
            else
            {
                charactersToRemove.Add(character);
                return true;
            }
        }

        /// <summary>
        /// Returns characters that go first in turn order.
        /// </summary>
        public List<CombatCharacter> GetInitiative()
        {
            return teams.SelectMany(o => o)
                .OrderBy(o => o.CombatSpeed.Value)
                .Reverse()
                .ToList();
        }

        /// <summary>
        /// Computes rounds of combat until only one team is left.
        /// </summary>
        /// <param name="beforeRound">
        /// An optional action that will be executed before each round begins.
        /// A list of all teams will be given.
        /// </param>
        public void ComputeCombat(Action<List<List<CombatCharacter>>> beforeRound)
        {
            while (true)
            {
                //Stops combat if all characters are on the same team.
                var allChars = teams.SelectMany(o => o).ToList();

                //Stops combat if there's one or no characters left.
                if (allChars.Count <= 1)
                {
                    break;
                }

                int teamId = allChars.Where(o => o != null).First().teamId;
                if (allChars.TrueForAll(o => o.teamId == teamId))
                {
                    //Reassigns all characters to different teams.
                    if (TeamFightsWhenDone)
                    {
                        for (int i = 0; i < allChars.Count; i++)
                        {
                            allChars[i].teamId = i;
                        }

                        teams.Clear();
                        teams.Add(allChars);
                    }

                    //Exits combat.
                    else
                    {
                        break;
                    }
                }

                beforeRound?.Invoke(new List<List<CombatCharacter>>(teams));
                ComputeCombatRound();
            }
        }

        /// <summary>
        /// Computes a full round of combat. Attempts to compute multiple
        /// rounds of combat at once will fail.
        /// </summary>
        public void ComputeCombatRound()
        {
            //Prevents concurrent execution.
            if (isInCombat)
            {
                return;
            }

            //Computes battle.
            isInCombat = true;
            List<CombatCharacter> charsInOrder = GetInitiative();
            for (int i = 0; i < charsInOrder.Count; i++)
            {
                //Updates the list of active combatants.
                for (int j = 0; j < charsInOrder.Count; j++)
                {
                    if ((bool)charsInOrder[j].RemoveFromCombat.Value)
                    {
                        RemoveFromCombat(charsInOrder[j]);
                    }
                }

                //Executes the actions for any character still in combat.
                if (!charactersToRemove.Contains(charsInOrder[i]))
                {
                    var activeChars = new List<CombatCharacter>(charsInOrder)
                        .Except(charactersToRemove);

                    currentCombatCharacter = charsInOrder[i];
                    currentCombatCharacter.CombatAction?.Invoke(
                        new List<CombatCharacter>(activeChars));
                }

                //Stops combat if set from within an invoked action.
                if (doStopCombat)
                {
                    break;
                }
            }

            isInCombat = false;
            currentCombatCharacter = null;

            //Handles queued items.
            for (int i = 0; i < charactersToAdd.Count; i++)
            {
                AddToCombat(charactersToAdd[i]);
            }
            for (int i = 0; i < charactersToRemove.Count; i++)
            {
                RemoveFromCombat(charactersToRemove[i]);
            }
            charactersToAdd.Clear();
            charactersToRemove.Clear();
        }

        /// <summary>
        /// When called during the execution of NextRound, this returns the
        /// character that's currently taking their turn. Returns null when
        /// combat is not in session.
        /// </summary>
        public CombatCharacter GetActiveCharacter()
        {
            return currentCombatCharacter;
        }

        /// <summary>
        /// Returns a list of all characters in combat at the moment.
        /// </summary>
        public List<CombatCharacter> GetCharacters()
        {
            return teams.SelectMany(o => o).ToList();
        }

        /// <summary>
        /// Returns all characters in combat at the moment, grouped by team.
        /// </summary>
        public List<List<CombatCharacter>> GetTeams()
        {
            return new List<List<CombatCharacter>>(teams);
        }

        /// <summary>
        /// Returns all characters on the team listed.
        /// </summary>
        public List<CombatCharacter> GetTeam(int teamId)
        {
            for (int i = 0; i < teams.Count; i++)
            {
                if (teams[i].Count > 0 && teams[i][0].teamId == teamId)
                {
                    return teams[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Returns all characters not on the listed team.
        /// </summary>
        public List<CombatCharacter> GetEnemyTeams(int teamId)
        {
            return teams.SelectMany(o => o)
                .Where((chr) => chr.teamId != teamId)
                .ToList();
        }

        /// <summary>
        /// When executed during a combat character's action, combat is stopped
        /// for all following characters.
        /// </summary>
        public void StopCombat()
        {
            doStopCombat = true;
        }
        #endregion
    }
}