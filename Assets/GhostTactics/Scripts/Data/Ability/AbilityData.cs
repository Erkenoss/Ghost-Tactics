using GhostTactics.Core;
using UnityEngine;

namespace GhostTactics.Data
{
    [CreateAssetMenu(fileName = "AbilityData", menuName = "GhostTactics/AbilityData")]
    public class AbilityData : ScriptableObject
    {
        #region Public Fields

        public Abilities Ability { get { return ability; } }
        public Sprite AbilityIcon { get { return abilityIcon; } }
        public bool CanBeRepeated { get { return canBeRepeated; } }

        #endregion

        #region Private Fields

        [Tooltip("Name of the ability")]
        [SerializeField]
        private Abilities ability = Abilities.none;

        [Tooltip("Sprite of the ability")]
        [SerializeField]
        private Sprite abilityIcon = null;

        [Tooltip("Is this Ability can be repeated?")]
        [SerializeField]
        private bool canBeRepeated = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}