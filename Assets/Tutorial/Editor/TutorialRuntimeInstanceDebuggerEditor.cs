using Tutorial.Runtime.Components;
using UnityEditor;
using UnityEngine;

namespace Tutorial.Editor
{
    /// <summary>
    /// Display runtime tutorial debugging controls inside the Unity Inspector
    /// </summary>
    [CustomEditor(typeof(TutorialRuntimeInstanceDebugger))]
    public sealed class TutorialRuntimeInstanceDebuggerEditor : UnityEditor.Editor
    {
        #region Inspector

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TutorialRuntimeInstanceDebugger debugger = target as TutorialRuntimeInstanceDebugger;

            if (debugger == null)
            {
                return;
            }

            EditorGUILayout.Space();

            DrawRuntimeStatus(debugger);
            DrawRuntimeGraphButton(debugger);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Display the current availability of the runtime tutorial instance
        /// </summary>
        /// <param name="debugger"></param>
        private static void DrawRuntimeStatus(TutorialRuntimeInstanceDebugger debugger)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("The runtime graph can only be inspected during Play Mode.", MessageType.Info);

                return;
            }

            if (!debugger.HasRuntimeInstance)
            {
                EditorGUILayout.HelpBox("No tutorial runtime instance is currently bound.", MessageType.Warning);

                return;
            }

            EditorGUILayout.HelpBox("A tutorial runtime instance is ready for inspection.", MessageType.None);
        }

        /// <summary>
        /// Display the button used to traverse and log the reconstructed graph
        /// </summary>
        /// <param name="debugger"></param>
        private static void DrawRuntimeGraphButton(TutorialRuntimeInstanceDebugger debugger)
        {
            bool canInspect = Application.isPlaying && debugger.HasRuntimeInstance;

            using (new EditorGUI.DisabledScope(!canInspect))
            {
                if (GUILayout.Button("Log Runtime Graph"))
                {
                    debugger.LogRuntimeGraph();
                }
            }
        }

        #endregion
    }
}