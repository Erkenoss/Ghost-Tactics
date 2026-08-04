using UnityEngine;

namespace Tutorial
{
    [CreateAssetMenu(fileName = "Step", menuName = "Tutorial/Step")]
    public class StepSO : ValidatorSO
    {
        #region Public Fields

        public EStepType StepType { get { return stepType; } set { stepType = value; } }
        public bool IsCompleted { get { return isCompleted; } set { isCompleted = value; } }
        public bool IsSkipped { get { return isSkipped; } set { isSkipped = value; } }
        public GameObject TutoTarget { get {  return tutoTarget; } set { tutoTarget = value; } }
        public MonoBehaviour ScriptTargeted { get { return scriptTargeted; }  set { scriptTargeted = value; } }
        public string MethodNameToCall { get { return methodNameToCall; } set { methodNameToCall = value; } }
        public string TutoGUID { get { return tutoGUID; } set { tutoGUID = value; } }
        public string StepGUID { get { return stepGUID; } set { stepGUID = value; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Type and category of this step
        /// </summary>
        [Tooltip("Type and category of this step")]
        [SerializeField]
        private EStepType stepType = EStepType.None;

        /// <summary>
        /// Use to  know if this step is completed
        /// </summary>
        [Tooltip("Use to  know if this step is completed")]
        [SerializeField]
        private bool isCompleted = false;

        /// <summary>
        /// Use to know if the player has skipped this step
        /// </summary>
        [Tooltip("Use to know if the player has skipped this step")]
        [SerializeField]
        private bool isSkipped = false;

        /// <summary>
        /// GameObject targeted by the Tutorial system to manage the reflection with the Monobehaviour
        /// </summary>
        [Tooltip("GameObject targeted by the Tutorial system to manage the reflection with the Monobehaviour")]
        [SerializeField]
        private GameObject tutoTarget = null;

        /// <summary>
        /// Script target by the step to raise the method name
        /// </summary>
        [Tooltip("Script target by the step to raise the method name")]
        [SerializeField]
        private MonoBehaviour scriptTargeted = null;

        /// <summary>
        /// Method we want to call and link to raised this tutorial step
        /// </summary>
        [Tooltip("Method we want to call and link to raised this tutorial step")]
        [SerializeField]
        private string methodNameToCall = string.Empty;

        /// <summary>
        /// Guid of the tuto to link with the stepGUID
        /// </summary>
        [Tooltip("Guid of the tuto to link with the stepGUID")]
        [SerializeField]
        private string tutoGUID = string.Empty;

        /// <summary>
        /// GUID identifier for the SO so link with the tutoGUID
        /// </summary>
        [SerializeField]
        private string stepGUID = string.Empty;

        // Gameobject, monobehaviour, string method name
        // Drag and drop, reflexivité => gameobject => component => method target string name
        // GUID du SO et du coup, le GUID => gameobject

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        public override void OnRaised()
        {
            TutoEventBus.Publish<OnRaised>(new OnRaised(this));
        }

        public override void OnSkipped()
        {
            TutoEventBus.Publish<OnSkipped>(new OnSkipped(this));
        }

        public override void OnTrigger()
        {
            TutoEventBus.Publish<OnTrigger>(new OnTrigger(this));
        }

        #endregion

        #region Private Methods
        #endregion
    }
}