using System;
using System.Collections.Generic;
using System.IO;
using Tutorial.Runtime.Catalogue;
using Tutorial.Runtime.Data;
using Tutorial.Runtime.Persistence;
using UnityEditor;
using UnityEngine;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Serializable root of the tutorial instrumentation manifest
    /// </summary>
    [Serializable]
    internal sealed class TutorialInjectionManifestData
    {
        #region Serialized Fields

        [SerializeField]
        private int version = 1;

        [SerializeField]
        private List<TutorialInjectionBindingData> bindings = new List<TutorialInjectionBindingData>();

        #endregion

        #region Properties

        public List<TutorialInjectionBindingData> Bindings => bindings;

        #endregion
    }

    /// <summary>
    /// Serializable method binding required by the IL Post Processor
    /// </summary>
    [Serializable]
    internal sealed class TutorialInjectionBindingData
    {
        #region Serialized Fields

        [SerializeField]
        private string graphGuid = string.Empty;

        [SerializeField]
        private string scriptName = string.Empty;

        [SerializeField]
        private string methodName = string.Empty;

        [SerializeField]
        private string stepGuid = string.Empty;

        #endregion

        #region Properties

        public string GraphGuid => graphGuid;
        public string ScriptName => scriptName;
        public string MethodName => methodName;
        public string StepGuid => stepGuid;

        #endregion

        #region Constructor

        public TutorialInjectionBindingData(string graphGuid, string scriptName, string methodName, string stepGuid)
        {
            this.graphGuid = graphGuid;
            this.scriptName = scriptName;
            this.methodName = methodName;
            this.stepGuid = stepGuid;
        }

        #endregion
    }

    /// <summary>
    /// Rebuild the tutorial method instrumentation manifest from runtime catalogues
    /// </summary>
    public sealed class TutorialInjectionManifestService
    {
        #region Constants

        private const string ManifestDirectoryPath = "ProjectSettings/Tutorial";
        private const string ManifestFilePath = ManifestDirectoryPath + "/TutorialInstrumentationManifest.json";

        #endregion

        #region Properties

        public string RelativeManifestPath => ManifestFilePath;

        #endregion

        #region Public Methods

        /// <summary>
        /// Rebuild the complete instrumentation manifest from every runtime catalogue
        /// </summary>
        /// <param name="manifestChanged"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryRebuild(out bool manifestChanged, out string failureReason)
        {
            manifestChanged = false;
            failureReason = string.Empty;

            try
            {
                TutorialInjectionManifestData manifest = new TutorialInjectionManifestData();
                HashSet<string> registeredBindings = new HashSet<string>(StringComparer.Ordinal);

                string[] catalogueGuids = AssetDatabase.FindAssets("t:TutorialRuntimeCatalogue");

                foreach (string catalogueGuid in catalogueGuids)
                {
                    string cataloguePath = AssetDatabase.GUIDToAssetPath(catalogueGuid);
                    TutorialRuntimeCatalogue catalogue = AssetDatabase.LoadAssetAtPath<TutorialRuntimeCatalogue>(cataloguePath);

                    if (catalogue == null || catalogue.Graphs == null)
                    {
                        continue;
                    }

                    foreach (TutorialRuntimeGraphEntry graphEntry in catalogue.Graphs)
                    {
                        if (graphEntry == null || graphEntry.Graph == null)
                        {
                            continue;
                        }

                        if (!TryCollectGraphBindings(graphEntry.Graph, manifest.Bindings, registeredBindings, out failureReason))
                        {
                            return false;
                        }
                    }
                }

                manifest.Bindings.Sort(CompareBindings);

                string json = JsonUtility.ToJson(manifest, true) + Environment.NewLine;
                string absoluteManifestPath = GetAbsoluteManifestPath();

                if (File.Exists(absoluteManifestPath))
                {
                    string currentJson = File.ReadAllText(absoluteManifestPath);

                    if (string.Equals(currentJson, json, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(absoluteManifestPath));
                File.WriteAllText(absoluteManifestPath, json);

                manifestChanged = true;

                return true;
            }
            catch (Exception exception)
            {
                failureReason = $"Unable to rebuild tutorial instrumentation manifest: {exception.Message}";

                return false;
            }
        }

        #endregion

        #region Binding Collection

        /// <summary>
        /// Collect every valid method binding contained by one tutorial graph
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="bindings"></param>
        /// <param name="registeredBindings"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private static bool TryCollectGraphBindings(TutorialGraphAsset graph, List<TutorialInjectionBindingData> bindings, HashSet<string> registeredBindings, out string failureReason)
        {
            failureReason = string.Empty;

            if (graph == null || graph.RuntimeNodeReferences == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(graph.GraphGuid))
            {
                failureReason = $"TutorialGraphAsset '{graph.name}' has no Graph GUID.";

                return false;
            }

            foreach (TutorialGraphRuntimeNodeReference runtimeReference in graph.RuntimeNodeReferences)
            {
                StepSO step = runtimeReference?.Step;

                if (step == null)
                {
                    continue;
                }

                bool hasScriptName = !string.IsNullOrWhiteSpace(step.ScriptName);
                bool hasMethodName = !string.IsNullOrWhiteSpace(step.MethodNameToCall);

                /*
                 * A StepSO without method binding does not require IL instrumentation.
                 */
                if (!hasScriptName && !hasMethodName)
                {
                    continue;
                }

                if (!hasScriptName || !hasMethodName)
                {
                    failureReason = $"StepSO '{step.name}' contains an incomplete method binding.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(step.StepGUID))
                {
                    failureReason = $"StepSO '{step.name}' has a method binding but no Step GUID.";

                    return false;
                }

                string graphGuid = graph.GraphGuid.Trim();
                string scriptName = step.ScriptName.Trim();
                string methodName = step.MethodNameToCall.Trim();
                string stepGuid = step.StepGUID.Trim();

                string bindingKey = $"{graphGuid}|{scriptName}|{methodName}|{stepGuid}";

                if (!registeredBindings.Add(bindingKey))
                {
                    continue;
                }

                bindings.Add(
                    new TutorialInjectionBindingData(
                        graphGuid,
                        scriptName,
                        methodName,
                        stepGuid
                    )
                );
            }

            return true;
        }

        /// <summary>
        /// Keep manifest bindings deterministically ordered
        /// </summary>
        private static int CompareBindings(TutorialInjectionBindingData left, TutorialInjectionBindingData right)
        {
            int result = string.CompareOrdinal(left?.GraphGuid, right?.GraphGuid);

            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left?.ScriptName, right?.ScriptName);

            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left?.MethodName, right?.MethodName);

            if (result != 0)
            {
                return result;
            }

            return string.CompareOrdinal(left?.StepGuid, right?.StepGuid);
        }

        #endregion

        #region Path

        /// <summary>
        /// Resolve the instrumentation manifest path from the Unity project root
        /// </summary>
        private static string GetAbsoluteManifestPath()
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ManifestFilePath));
        }

        #endregion
    }
}