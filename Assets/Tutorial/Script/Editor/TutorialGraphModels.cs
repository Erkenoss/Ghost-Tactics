using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial
{
    /// <summary>
    /// Visual relation between a StepSO and a GameObject
    /// containing a TutoIdentifier
    /// </summary>
    internal sealed class BindingConnection
    {
        #region Properties

        /// <summary>
        /// StepSO associated with the binding
        /// </summary>
        public StepSO Step { get; }

        /// <summary>
        /// Visual node representing the StepSO
        /// </summary>
        public VisualElement SourceNode { get; }

        /// <summary>
        /// Port used by the StepSO binding output
        /// </summary>
        public VisualElement SourcePort { get; }

        /// <summary>
        /// Visual node representing the target GameObject
        /// </summary>
        public VisualElement TargetNode { get; }

        /// <summary>
        /// Port used by the target GameObject input
        /// </summary>
        public VisualElement TargetPort { get; }

        /// <summary>
        /// GameObject represented by the target node
        /// </summary>
        public GameObject TargetGameObject => TargetNode?.userData as GameObject;

        /// <summary>
        /// Whether every required reference is available
        /// </summary>
        public bool IsValid => Step != null && SourceNode != null && SourceNode.userData is StepSO sourceStep && SourcePort != null && TargetNode != null && TargetGameObject != null && TargetPort != null;

        #endregion

        #region Constructor

        public BindingConnection(StepSO step, VisualElement sourceNode, VisualElement sourcePort, VisualElement targetNode, VisualElement targetPort)
        {
            Step = step;
            SourceNode = sourceNode;
            SourcePort = sourcePort;
            TargetNode = targetNode;
            TargetPort = targetPort;
        }

        #endregion
    }

    #region Sequence Connection

    /// <summary>
    /// Visual relation between two StepSO nodes
    /// belonging to a StepSequenceSO
    /// </summary>
    internal sealed class SequenceConnection
    {
        #region Properties

        /// <summary>
        /// Sequence containing the connected StepSO
        /// </summary>
        public StepSequenceSO Sequence { get; }

        /// <summary>
        /// Visual node representing the source StepSO
        /// </summary>
        public VisualElement SourceNode { get; }

        /// <summary>
        /// Sequence output port of the source node
        /// </summary>
        public VisualElement SourcePort { get; }

        /// <summary>
        /// Visual node representing the target StepSO
        /// </summary>
        public VisualElement TargetNode { get; }

        /// <summary>
        /// Sequence input port of the target node
        /// </summary>
        public VisualElement TargetPort { get; }

        /// <summary>
        /// StepSO represented by the source node
        /// </summary>
        public StepSO SourceStep => SourceNode?.userData as StepSO;

        /// <summary>
        /// StepSO represented by the target node
        /// </summary>
        public StepSO TargetStep => TargetNode?.userData as StepSO; 

        /// <summary>
        /// Whether every required reference is available
        /// </summary>
        public bool IsValid => Sequence != null && SourceNode != null && SourceStep != null && SourcePort != null && TargetNode != null && TargetStep != null && TargetPort != null;

        #endregion

        #region Constructor

        public SequenceConnection(StepSequenceSO sequence, VisualElement sourceNode, VisualElement sourcePort, VisualElement targetNode, VisualElement targetPort)
        {
            Sequence = sequence;
            SourceNode = sourceNode;
            SourcePort = sourcePort;
            TargetNode = targetNode;
            TargetPort = targetPort;
        }

        #endregion
    }

    #endregion

    #region Method Binding

    /// <summary>
    /// Association between a MonoBehaviour and one of its
    /// callable public methods
    /// </summary>
    internal sealed class MethodBindingOption
    {
        #region Properties

        /// <summary>
        /// MonoBehaviour declaring the method
        /// </summary>
        public MonoBehaviour Script { get; }

        /// <summary>
        /// Public method available for the tutorial binding
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Text displayed inside the editor dropdown
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Full script type name stored inside the StepSO
        /// </summary>
        public string StoredScriptName { get; }

        /// <summary>
        /// Method name stored inside the StepSO
        /// </summary>
        public string StoredMethodName => Method?.Name ?? string.Empty;

        /// <summary>
        /// Whether the script and method references are valid
        /// </summary>
        public bool IsValid => Script != null && Method != null && !string.IsNullOrWhiteSpace(StoredScriptName);

        #endregion

        #region Constructor

        public MethodBindingOption(MonoBehaviour script, MethodInfo method)
        {
            Script = script;
            Method = method;

            if (script == null || method == null)
            {
                DisplayName = "Invalid method";
                StoredScriptName = string.Empty;

                return;
            }

            StoredScriptName = script.GetType().FullName;

            DisplayName = $"{script.GetType().Name} / {method.Name}()";
        }

        #endregion
    }

    #endregion
}