using UnityEngine;
using System.Collections.Generic;

namespace GhostTactics.Data
{
    [CreateAssetMenu(fileName = "EnnemyData", menuName = "GhostTactics/EnnemyData")]
    public class EnnemyData : ScriptableObject
    {
        #region Public Fields

        public List<AbilityData> Abilities { get { return abilities; } }
        public int EnnemyHealth {  get { return ennemyHealth; } }
        public bool IsBoss { get { return isBoss; } }

        #endregion

        #region Private Fields

        [Tooltip("List of all the abilities of the ennemy")]
        [SerializeField]
        private List<AbilityData> abilities = new List<AbilityData>();

        [Tooltip("health of the ennemy")]
        [SerializeField]
        private int ennemyHealth = 0;

        [Tooltip("Use to know if the player fight against a boss")]
        [SerializeField]
        private bool isBoss = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}