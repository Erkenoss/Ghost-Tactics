using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Tutorial.Editor.Core
{
    /// <summary>
    /// Runtime representation of a node currently displayed inside the tutorial graph
    /// </summary>
    public sealed class TutorialRuntimeNode
    {
        #region Properties

        /// <summary>
        /// Persistent identifier of the visual graph node
        /// </summary>
        public string NodeGuid { get; }

        /// <summary>
        /// Unity object represented by the graph node
        /// </summary>
        public UnityObject Target { get; }

        /// <summary>
        /// VisualElement currently representing the graph node
        /// </summary>
        public VisualElement Element { get; }

        /// <summary>
        /// Whether every required runtime reference is available
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(NodeGuid) && Target != null && Element != null;

        #endregion

        #region Constructor

        internal TutorialRuntimeNode(string nodeGuid, UnityObject target, VisualElement element)
        {
            NodeGuid = nodeGuid;
            Target = target;
            Element = element;
        }

        #endregion
    }

    /// <summary>
    /// Maintain the correspondence between persistent node GUIDs,
    /// Unity objects and their current visual elements
    /// </summary>
    public sealed class TutorialGraphRuntimeRegistry
    {
        #region Private Fields

        /// <summary>
        /// Nodes indexed by their persistent graph identifier
        /// </summary>
        private readonly Dictionary<string, TutorialRuntimeNode> nodesByGuid = new Dictionary<string, TutorialRuntimeNode>(StringComparer.Ordinal);

        /// <summary>
        /// Nodes indexed by their current visual representation
        /// </summary>
        private readonly Dictionary<VisualElement, TutorialRuntimeNode> nodesByElement = new Dictionary<VisualElement, TutorialRuntimeNode>();

        #endregion

        #region Properties

        /// <summary>
        /// Number of nodes currently registered
        /// </summary>
        public int Count => nodesByGuid.Count;

        /// <summary>
        /// Whether the registry currently contains no node
        /// </summary>
        public bool IsEmpty => nodesByGuid.Count == 0;

        /// <summary>
        /// Every node currently registered
        /// </summary>
        public IEnumerable<TutorialRuntimeNode> Nodes => nodesByGuid.Values;

        #endregion

        #region Registration

        /// <summary>
        /// Register a visual graph node
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="target"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public bool TryRegister(string nodeGuid, UnityObject target, VisualElement element)
        {
            nodeGuid = NormalizeNodeGuid(nodeGuid);

            if (string.IsNullOrWhiteSpace(nodeGuid) || target == null || element == null)
            {
                return false;
            }

            if (nodesByGuid.ContainsKey(nodeGuid))
            {
                return false;
            }

            if (nodesByElement.ContainsKey(element))
            {
                return false;
            }

            TutorialRuntimeNode runtimeNode = new TutorialRuntimeNode(nodeGuid, target, element);

            nodesByGuid.Add(nodeGuid, runtimeNode);
            nodesByElement.Add(element, runtimeNode);

            return true;
        }

        /// <summary>
        /// Remove a node using its persistent graph identifier
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        public bool TryUnregister(string nodeGuid)
        {
            nodeGuid = NormalizeNodeGuid(nodeGuid);

            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                return false;
            }

            if (!nodesByGuid.TryGetValue(nodeGuid, out TutorialRuntimeNode runtimeNode))
            {
                return false;
            }

            RemoveNode(runtimeNode);

            return true;
        }

        /// <summary>
        /// Remove a node using its visual element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public bool TryUnregister(VisualElement element)
        {
            if (element == null)
            {
                return false;
            }

            if (!nodesByElement.TryGetValue(element, out TutorialRuntimeNode runtimeNode))
            {
                return false;
            }

            RemoveNode(runtimeNode);

            return true;
        }

        /// <summary>
        /// Remove every registered runtime node
        /// </summary>
        public void Clear()
        {
            nodesByGuid.Clear();
            nodesByElement.Clear();
        }

        /// <summary>
        /// Remove a runtime node from every index
        /// </summary>
        /// <param name="runtimeNode"></param>
        private void RemoveNode(TutorialRuntimeNode runtimeNode)
        {
            if (runtimeNode == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(runtimeNode.NodeGuid))
            {
                nodesByGuid.Remove(runtimeNode.NodeGuid);
            }

            if (runtimeNode.Element != null)
            {
                nodesByElement.Remove(runtimeNode.Element);
            }
        }

        #endregion

        #region GUID Lookup

        /// <summary>
        /// Find a runtime node using its persistent graph identifier
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        public bool TryGetNode(string nodeGuid, out TutorialRuntimeNode runtimeNode)
        {
            nodeGuid = NormalizeNodeGuid(nodeGuid);

            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                runtimeNode = null;

                return false;
            }

            return nodesByGuid.TryGetValue(nodeGuid, out runtimeNode);
        }

        /// <summary>
        /// Find the visual element associated with a persistent node identifier
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public bool TryGetElement(string nodeGuid, out VisualElement element)
        {
            element = null;

            if (!TryGetNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
            {
                return false;
            }

            element = runtimeNode.Element;

            return element != null;
        }

        /// <summary>
        /// Check whether a persistent node identifier is registered
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        public bool ContainsGuid(string nodeGuid)
        {
            nodeGuid = NormalizeNodeGuid(nodeGuid);

            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                return false;
            }

            return nodesByGuid.ContainsKey(nodeGuid);
        }

        #endregion

        #region VisualElement Lookup

        /// <summary>
        /// Find a runtime node using its visual element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        public bool TryGetNode(VisualElement element, out TutorialRuntimeNode runtimeNode)
        {
            if (element == null)
            {
                runtimeNode = null;

                return false;
            }

            return nodesByElement.TryGetValue(element, out runtimeNode);
        }

        /// <summary>
        /// Find the persistent identifier associated with a visual element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        public bool TryGetNodeGuid(VisualElement element, out string nodeGuid)
        {
            nodeGuid = string.Empty;

            if (!TryGetNode(element, out TutorialRuntimeNode runtimeNode))
            {
                return false;
            }

            nodeGuid = runtimeNode.NodeGuid;

            return !string.IsNullOrWhiteSpace(nodeGuid);
        }

        /// <summary>
        /// Check whether a visual element is registered
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public bool ContainsElement(VisualElement element)
        {
            if (element == null)
            {
                return false;
            }

            return nodesByElement.ContainsKey(element);
        }

        #endregion

        #region Target Lookup

        /// <summary>
        /// Find every graph node representing a given Unity object
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public IReadOnlyList<TutorialRuntimeNode> GetNodesByTarget(UnityObject target)
        {
            List<TutorialRuntimeNode> matchingNodes = new List<TutorialRuntimeNode>();

            if (target == null)
            {
                return matchingNodes;
            }

            foreach (TutorialRuntimeNode runtimeNode in nodesByGuid.Values)
            {
                if (runtimeNode == null || runtimeNode.Target != target)
                {
                    continue;
                }

                matchingNodes.Add(runtimeNode);
            }

            return matchingNodes;
        }

        /// <summary>
        /// Check whether at least one graph node represents a given Unity object
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool ContainsTarget(UnityObject target)
        {
            if (target == null)
            {
                return false;
            }

            foreach (TutorialRuntimeNode runtimeNode in nodesByGuid.Values)
            {
                if (runtimeNode != null && runtimeNode.Target == target)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Normalize a graph node identifier
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        private static string NormalizeNodeGuid(string nodeGuid)
        {
            return string.IsNullOrWhiteSpace(nodeGuid) ? string.Empty : nodeGuid.Trim();
        }

        #endregion
    }
}