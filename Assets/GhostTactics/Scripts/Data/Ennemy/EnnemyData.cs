using UnityEngine;
using System.Collections.Generic;

namespace GhostTactics.Data
{
    [CreateAssetMenu(fileName = "EnnemyData", menuName = "GhostTactics/EnnemyData")]
    public class EnnemyData : ScriptableObject
    {
        #region Public Fields

        public List<AbilityData> Abilities { get { return abilities; } }

        #endregion

        #region Private Fields

        [Tooltip("List of all the abilities of the ennemy")]
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