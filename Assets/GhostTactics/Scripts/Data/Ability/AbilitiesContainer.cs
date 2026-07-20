using UnityEngine;
using System.Collections.Generic;

namespace GhostTactics.Data
{
    [CreateAssetMenu(fileName = "AbilitiesContainer", menuName = "GhostTactics/AbilitiesContainer")]
    public class AbilitiesContainer : ScriptableObject
    {
        #region Public Fields

        public List<AbilityData> Abilities { get { return abilities; } }

        #endregion

        #region Private Fields

        [Tooltip("List of abilities available in the game.")]
        [SerializeField]
        private List<AbilityData> abilities = new List<AbilityData>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the AbilityData object corresponding to the given ability name.
        /// </summary>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public AbilityData GetAbilityByName(string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName))
            {
                return null;
            }
            
            return abilities.Find(ability => ability.Ability.ToString() == abilityName);
        }

        #endregion

        #region Private Methods
        #endregion
    }
}