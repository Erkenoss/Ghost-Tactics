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
        #endregion

        #region Private Methods
        #endregion
    }
}