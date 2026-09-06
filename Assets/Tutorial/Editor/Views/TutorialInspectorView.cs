using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityObject = UnityEngine.Object;

using Tutorial.Runtime.Data;
using Tutorial.Runtime.Components;

using Tutorial.Editor.Core;
using Tutorial.Editor.Services;

using Tutorial.Runtime.Data.Completion;
using Tutorial.Runtime.Data.Completion.UI;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Display the inspector associated with the currently selected tutorial graph element
    /// </summary>
    internal sealed class TutorialInspectorView
    {
        #region Constants

        private const string DefaultMethodChoice = "Select a method...";
        private const string EmptyMethodChoice = "No compatible public method";

        #endregion

        #region Colors

        private static readonly Color SecondaryTextColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ContainerBackgroundColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color WarningTextColor = new Color(1f, 0.55f, 0.3f);
        private static readonly Color ErrorTextColor = new Color(1f, 0.4f, 0.4f);
        
        /// <summary>
        /// Color used for valid tutorial configuration messages
        /// </summary>
        private static readonly Color ValidTextColor = new Color(0.4f, 0.8f, 0.45f);

        #endregion

        #region Private Fields

        /// <summary>
        /// Root inspector panel
        /// </summary>
        private readonly ScrollView inspectorPanel = null;

        /// <summary>
        /// Service responsible for tutorial GUID operations
        /// </summary>
        private readonly TutorialGuidService guidService = null;

        /// <summary>
        /// Service responsible for script and method binding
        /// </summary>
        private readonly TutorialMethodBindingService methodBindingService = null;

        /// <summary>
        /// SerializedObject currently bound to an InspectorElement
        /// </summary>
        private SerializedObject selectedSerializedObject = null;

        #endregion

        #region Constructor

        public TutorialInspectorView(ScrollView inspectorPanel, TutorialGuidService guidService, TutorialMethodBindingService methodBindingService)
        {
            this.inspectorPanel = inspectorPanel ?? throw new ArgumentNullException(nameof(inspectorPanel));
            this.guidService = guidService ?? throw new ArgumentNullException(nameof(guidService));
            this.methodBindingService = methodBindingService ?? throw new ArgumentNullException(nameof(methodBindingService));
        }

        #endregion

        #region Placeholder

        /// <summary>
        /// Display the empty inspector message
        /// </summary>
        public void DisplayPlaceholder()
        {
            ClearPanel();

            Label placeholder = new Label("Select a tutorial graph element");

            placeholder.style.flexGrow = 1f;
            placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            placeholder.style.whiteSpace = WhiteSpace.Normal;
            placeholder.style.color = new Color(0.55f, 0.55f, 0.55f);

            inspectorPanel.Add(placeholder);
        }

        #endregion

        #region Step Inspector

        /// <summary>
        /// Display the inspector of a StepSO
        /// </summary>
        /// <param name="step"></param>
        public void DisplayStep(StepSO step)
        {
            ClearPanel();

            if (step == null)
            {
                DisplayInvalidTarget("The selected StepSO is missing.");
                return;
            }

            inspectorPanel.Add(CreateObjectButton(step));
            inspectorPanel.Add(CreateSubtitle(step.GetType().Name));

            Button generateGuidButton = new Button(() => GenerateStepGuid(step))
            {
                text = string.IsNullOrWhiteSpace(step.StepGUID) ? "Generate Step GUID" : "Step GUID already generated"
            };

            generateGuidButton.style.marginTop = 4f;
            generateGuidButton.style.marginBottom = 8f;
            generateGuidButton.SetEnabled(string.IsNullOrWhiteSpace(step.StepGUID));

            inspectorPanel.Add(generateGuidButton);

            selectedSerializedObject = new SerializedObject(step);
            selectedSerializedObject.Update();

            SerializedProperty stepTypeProperty = selectedSerializedObject.FindProperty("stepType");

            InspectorElement inspectorElement = new InspectorElement
            {
                name = "tutorial-step-inspector"
            };

            inspectorElement.Bind(selectedSerializedObject);

            if (stepTypeProperty != null)
            {
                inspectorElement.TrackPropertyValue(stepTypeProperty, property => DisplayStep(step));
            }

            inspectorPanel.Add(inspectorElement);

            if (step.StepType == EStepType.UI)
            {
                DisplayUICompletion(step);
            }
        }

        /// <summary>
        /// Generate the GUID of a StepSO and refresh its inspector
        /// </summary>
        /// <param name="step"></param>
        private void GenerateStepGuid(StepSO step)
        {
            if (step == null)
            {
                return;
            }

            if (!guidService.TryGenerateStepGuid(step))
            {
                return;
            }

            DisplayStep(step);
        }

        #endregion

        #region GameObject Inspector

        /// <summary>
        /// Display every StepSO connected to a GameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="linkedSteps"></param>
        public void DisplayGameObject(GameObject gameObject, IReadOnlyList<StepSO> linkedSteps)
        {
            ClearPanel();

            if (gameObject == null)
            {
                DisplayInvalidTarget("The selected GameObject is missing.");

                return;
            }

            inspectorPanel.Add(CreateObjectButton(gameObject));
            inspectorPanel.Add(CreateSubtitle("Connected Steps"));

            DisplayIdentifierInformation(gameObject, linkedSteps);

            if (linkedSteps == null || linkedSteps.Count == 0)
            {
                inspectorPanel.Add(CreateInformationLabel("No StepSO is connected to this GameObject."));

                return;
            }

            IReadOnlyList<MethodBindingOption> methodOptions = methodBindingService.GetBindingOptions(gameObject) ?? Array.Empty<MethodBindingOption>();

            foreach (StepSO step in linkedSteps)
            {
                if (step == null)
                {
                    continue;
                }

                inspectorPanel.Add(CreateConnectedStepEntry(step, methodOptions));
            }
        }

        /// <summary>
        /// Display the TutoIdentifier information of a GameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="linkedSteps"></param>
        private void DisplayIdentifierInformation(GameObject gameObject, IReadOnlyList<StepSO> linkedSteps)
        {
            if (!gameObject.TryGetComponent(out TutoIdentifier identifier))
            {
                Label missingIdentifier = CreateInformationLabel("No TutoIdentifier was found on this GameObject.");

                missingIdentifier.style.color = ErrorTextColor;
                inspectorPanel.Add(missingIdentifier);

                return;
            }

            VisualElement identifierContainer = CreateSectionContainer();
            Label identifierTitle = new Label("Tutorial Identifier");
            Label identifierGuid = new Label(string.IsNullOrWhiteSpace(identifier.ObjectGUID) ? "GUID: Not generated" : $"GUID: {identifier.ObjectGUID}");
            ObjectField targetComponentField = new ObjectField("Target Component")
            {
                objectType = typeof(Component),
                allowSceneObjects = true,
                value = identifier.TargetComponent
            };

            identifierTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            identifierTitle.style.marginBottom = 4f;

            identifierGuid.style.whiteSpace = WhiteSpace.Normal;
            identifierGuid.style.color = SecondaryTextColor;

            targetComponentField.style.marginTop = 6f;
            targetComponentField.RegisterValueChangedCallback(changeEvent => OnTargetComponentChanged(gameObject, identifier, linkedSteps, changeEvent.newValue as Component));

            identifierContainer.Add(identifierTitle);
            identifierContainer.Add(identifierGuid);
            identifierContainer.Add(targetComponentField);

            DisplayUIComponentValidation(identifier, linkedSteps, identifierContainer);

            inspectorPanel.Add(identifierContainer);
        }

        /// <summary>
        /// Handle a modification of the Component targeted by a TutoIdentifier
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="identifier"></param>
        /// <param name="linkedSteps"></param>
        /// <param name="targetComponent"></param>
        private void OnTargetComponentChanged(GameObject gameObject, TutoIdentifier identifier, IReadOnlyList<StepSO> linkedSteps, Component targetComponent)
        {
            if (!SetTargetComponent(identifier, targetComponent))
            {
                return;
            }

            DisplayGameObject(gameObject, linkedSteps);
        }

        /// <summary>
        /// Set the Component targeted by a TutoIdentifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="targetComponent"></param>
        /// <returns></returns>
        private static bool SetTargetComponent(TutoIdentifier identifier, Component targetComponent)
        {
            if (identifier == null || identifier.TargetComponent == targetComponent)
            {
                return false;
            }

            Undo.RecordObject(identifier, "Set tutorial target component");

            identifier.TargetComponent = targetComponent;

            EditorUtility.SetDirty(identifier);
            PrefabUtility.RecordPrefabInstancePropertyModifications(identifier);

            if (identifier.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(identifier.gameObject.scene);
            }

            return true;
        }

        /// <summary>
        /// Display the validation state of the target Component for every connected UI Step
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="linkedSteps"></param>
        /// <param name="container"></param>
        private void DisplayUIComponentValidation(TutoIdentifier identifier, IReadOnlyList<StepSO> linkedSteps, VisualElement container)
        {
            if (identifier == null || linkedSteps == null || container == null)
            {
                return;
            }

            foreach (StepSO step in linkedSteps)
            {
                if (step == null || step.StepType != EStepType.UI)
                {
                    continue;
                }

                if (!(step.CompletionData is TutorialUICompletionData uiData))
                {
                    Label missingConfiguration = CreateInformationLabel($"{step.name}: UI completion data is missing.");

                    missingConfiguration.style.color = ErrorTextColor;
                    container.Add(missingConfiguration);

                    continue;
                }

                DisplayUIComponentValidation(step, uiData, identifier.TargetComponent, container);
            }
        }

        /// <summary>
        /// Display the validation state of a target Component for a UI Step
        /// </summary>
        /// <param name="step"></param>
        /// <param name="uiData"></param>
        /// <param name="targetComponent"></param>
        /// <param name="container"></param>
        private void DisplayUIComponentValidation(StepSO step, TutorialUICompletionData uiData, Component targetComponent, VisualElement container)
        {
            if (step == null || uiData == null || container == null)
            {
                return;
            }

            string expectedComponentName = TutorialUIComponentService.GetExpectedComponentName(uiData.ElementType);

            if (targetComponent == null)
            {
                Label missingTarget = CreateInformationLabel($"{step.name}: Missing target. Expected: {expectedComponentName}.");

                missingTarget.style.color = WarningTextColor;
                container.Add(missingTarget);

                return;
            }

            if (!TutorialUIComponentService.IsCompatible(uiData.ElementType, targetComponent))
            {
                Label invalidTarget = CreateInformationLabel($"{step.name}: Invalid target. Expected: {expectedComponentName}. Found: {targetComponent.GetType().Name}.");

                invalidTarget.style.color = ErrorTextColor;
                container.Add(invalidTarget);

                return;
            }

            Label validTarget = CreateInformationLabel($"{step.name}: Valid UI target ({targetComponent.GetType().Name}).");

            validTarget.style.color = ValidTextColor;
            container.Add(validTarget);
        }

        /// <summary>
        /// Create the method selection entry of a connected StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <param name="methodOptions"></param>
        /// <returns></returns>
        private VisualElement CreateConnectedStepEntry(StepSO step, IReadOnlyList<MethodBindingOption> methodOptions)
        {
            VisualElement container = CreateSectionContainer();
            Button stepButton = CreateObjectButton(step);
            List<string> choices = CreateMethodChoices(methodOptions);
            int selectedIndex = methodBindingService.GetCurrentBindingIndex(step, methodOptions);

            selectedIndex = Mathf.Clamp(selectedIndex, 0, choices.Count - 1);

            DropdownField methodDropdown = new DropdownField(string.Empty, choices, selectedIndex)
            {
                name = "tutorial-method-binding-dropdown"
            };

            methodDropdown.style.flexGrow = 1f;
            methodDropdown.style.marginTop = 4f;

            if (methodOptions == null || methodOptions.Count == 0)
            {
                methodDropdown.SetEnabled(false);
            }
            else
            {
                methodDropdown.RegisterValueChangedCallback(changeEvent => OnMethodSelected(step, methodOptions, choices, methodDropdown));
            }

            container.Add(stepButton);
            container.Add(methodDropdown);

            return container;
        }

        /// <summary>
        /// Handle the selection of a script method
        /// </summary>
        /// <param name="step"></param>
        /// <param name="methodOptions"></param>
        /// <param name="choices"></param>
        /// <param name="methodDropdown"></param>
        private void OnMethodSelected(StepSO step, IReadOnlyList<MethodBindingOption> methodOptions, IReadOnlyList<string> choices, DropdownField methodDropdown)
        {
            if (step == null || methodOptions == null || choices == null || methodDropdown == null)
            {
                return;
            }

            int optionIndex = methodDropdown.index - 1;

            if (optionIndex < 0 || optionIndex >= methodOptions.Count)
            {
                return;
            }

            MethodBindingOption option = methodOptions[optionIndex];

            if (option == null || !option.IsValid)
            {
                RestoreMethodDropdown(step, methodOptions, choices, methodDropdown);

                return;
            }

            if (!methodBindingService.TrySetBinding(step, option))
            {
                RestoreMethodDropdown(step, methodOptions, choices, methodDropdown);
            }
        }

        /// <summary>
        /// Restore the method dropdown to the binding stored inside the StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <param name="methodOptions"></param>
        /// <param name="choices"></param>
        /// <param name="methodDropdown"></param>
        private void RestoreMethodDropdown(StepSO step, IReadOnlyList<MethodBindingOption> methodOptions, IReadOnlyList<string> choices, DropdownField methodDropdown)
        {
            int restoredIndex = methodBindingService.GetCurrentBindingIndex(step, methodOptions);
            restoredIndex = Mathf.Clamp(restoredIndex, 0, choices.Count - 1);

            methodDropdown.SetValueWithoutNotify(choices[restoredIndex]);
        }

        /// <summary>
        /// Create the labels displayed inside a method dropdown
        /// </summary>
        /// <param name="methodOptions"></param>
        /// <returns></returns>
        private static List<string> CreateMethodChoices(IReadOnlyList<MethodBindingOption> methodOptions)
        {
            List<string> choices = new List<string>();

            if (methodOptions == null || methodOptions.Count == 0)
            {
                choices.Add(EmptyMethodChoice);

                return choices;
            }

            choices.Add(DefaultMethodChoice);

            foreach (MethodBindingOption option in methodOptions)
            {
                if (option == null || !option.IsValid)
                {
                    continue;
                }

                choices.Add(option.DisplayName);
            }

            return choices;
        }

        #endregion


        #region UI COmpletion Inspector

        /// <summary>
        /// Display the UI completion configuration of a StepSO
        /// </summary>
        /// <param name="step"></param>
        private void DisplayUICompletion(StepSO step)
        {
            if (step == null || selectedSerializedObject == null)
            {
                return;
            }

            VisualElement container = CreateSectionContainer();
            Label title = new Label("UI Completion");

            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4f;

            EUIElementType currentElementType = GetUIElementType(step);
            EnumField elementTypeField = new EnumField("UI Element", currentElementType);

            elementTypeField.RegisterValueChangedCallback(changeEvent => OnUIElementTypeChanged(step, (EUIElementType)changeEvent.newValue));

            container.Add(title);
            container.Add(elementTypeField);

            DisplayUICompletionParameters(step, container);

            inspectorPanel.Add(container);
        }

        /// <summary>
        /// Handle a modification of the UI element type of a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <param name="elementType"></param>
        private void OnUIElementTypeChanged(StepSO step, EUIElementType elementType)
        {
            if (step == null)
            {
                return;
            }

            SerializedObject serializedStep = new SerializedObject(step);
            SerializedProperty completionDataProperty = serializedStep.FindProperty("completionData");

            if (completionDataProperty == null)
            {
                Debug.LogError($"The completion data property was not found on '{step.name}'.");
                return;
            }

            serializedStep.Update();
            completionDataProperty.managedReferenceValue = CreateUICompletionData(elementType);
            serializedStep.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(step);

            DisplayStep(step);
        }

        /// <summary>
        /// Create the completion data associated with a UI element type
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        private static TutorialCompletionData CreateUICompletionData(EUIElementType elementType)
        {
            switch (elementType)
            {
                case EUIElementType.Button:
                    return new TutorialUIButtonCompletionData();

                case EUIElementType.Toggle:
                    return new TutorialUIToggleCompletionData();

                case EUIElementType.Slider:
                    return new TutorialUISliderCompletionData();

                case EUIElementType.Scrollbar:
                    return new TutorialUIScrollbarCompletionData();

                case EUIElementType.Dropdown:
                    return new TutorialUIDropdownCompletionData();

                case EUIElementType.InputField:
                    return new TutorialUIInputFieldCompletionData();

                case EUIElementType.ScrollRect:
                    return new TutorialUIScrollRectCompletionData();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Display the parameters associated with the current UI completion data
        /// </summary>
        /// <param name="step"></param>
        /// <param name="container"></param>
        private void DisplayUICompletionParameters(StepSO step, VisualElement container)
        {
            if (step == null || container == null || selectedSerializedObject == null)
            {
                return;
            }

            TutorialUICompletionData uiData = step.CompletionData as TutorialUICompletionData;

            if (uiData == null)
            {
                container.Add(CreateInformationLabel("Select a UI element type."));
                return;
            }

            SerializedProperty completionDataProperty = selectedSerializedObject.FindProperty("completionData");

            if (completionDataProperty == null)
            {
                return;
            }

            switch (uiData.ElementType)
            {
                case EUIElementType.Button:
                    container.Add(CreateInformationLabel("Button click requires no additional parameter."));
                    break;

                case EUIElementType.Toggle:
                    AddTrackedUIProperty(container, completionDataProperty, "completionMode", "Completion Mode", step);

                    if (uiData is TutorialUIToggleCompletionData toggleData && toggleData.CompletionMode == EUIValueCompletionMode.ExpectedValue)
                    {
                        AddUIProperty(container, completionDataProperty, "expectedValue", "Expected Value");
                    }

                    break;

                case EUIElementType.Slider:
                    AddTrackedUIProperty(container, completionDataProperty, "completionMode", "Completion Mode", step);

                    if (uiData is TutorialUISliderCompletionData sliderData && sliderData.CompletionMode == EUIValueCompletionMode.ExpectedValue)
                    {
                        AddUIProperty(container, completionDataProperty, "comparisonType", "Comparison");
                        AddUIProperty(container, completionDataProperty, "expectedValue", "Expected Value");
                    }

                    break;

                case EUIElementType.Scrollbar:
                    AddTrackedUIProperty(container, completionDataProperty, "completionMode", "Completion Mode", step);

                    if (uiData is TutorialUIScrollbarCompletionData scrollbarData && scrollbarData.CompletionMode == EUIValueCompletionMode.ExpectedValue)
                    {
                        AddUIProperty(container, completionDataProperty, "comparisonType", "Comparison");
                        AddUIProperty(container, completionDataProperty, "expectedValue", "Expected Value");
                    }

                    break;

                case EUIElementType.Dropdown:
                    AddTrackedUIProperty(container, completionDataProperty, "completionMode", "Completion Mode", step);

                    if (uiData is TutorialUIDropdownCompletionData dropdownData && dropdownData.CompletionMode == EUIValueCompletionMode.ExpectedValue)
                    {
                        AddUIProperty(container, completionDataProperty, "expectedIndex", "Expected Index");
                    }

                    break;

                case EUIElementType.InputField:
                    AddTrackedUIProperty(container, completionDataProperty, "completionMode", "Completion Mode", step);

                    if (uiData is TutorialUIInputFieldCompletionData inputFieldData && inputFieldData.CompletionMode == EUITextCompletionMode.ExpectedText)
                    {
                        AddUIProperty(container, completionDataProperty, "expectedText", "Expected Text");
                    }

                    break;

                case EUIElementType.ScrollRect:
                    AddTrackedUIProperty(container, completionDataProperty, "completionMode", "Completion Mode", step);

                    if (uiData is TutorialUIScrollRectCompletionData scrollRectData && scrollRectData.CompletionMode == EUIScrollCompletionMode.ExpectedPosition)
                    {
                        AddUIProperty(container, completionDataProperty, "axis", "Axis");
                        AddUIProperty(container, completionDataProperty, "comparisonType", "Comparison");
                        AddUIProperty(container, completionDataProperty, "expectedPosition", "Expected Position");
                    }

                    break;
            }
        }

        /// <summary>
        /// Add a serialized UI completion property to a container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="parentProperty"></param>
        /// <param name="propertyName"></param>
        /// <param name="label"></param>
        private static void AddUIProperty(VisualElement container, SerializedProperty parentProperty, string propertyName, string label)
        {
            if (container == null || parentProperty == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            SerializedProperty property = parentProperty.FindPropertyRelative(propertyName);

            if (property == null)
            {
                return;
            }

            PropertyField propertyField = new PropertyField(property, label);
            propertyField.BindProperty(property);

            container.Add(propertyField);
        }

        /// <summary>
        /// Add a serialized UI completion property and refresh the Step inspector when its value changes
        /// </summary>
        /// <param name="container"></param>
        /// <param name="parentProperty"></param>
        /// <param name="propertyName"></param>
        /// <param name="label"></param>
        /// <param name="step"></param>
        private void AddTrackedUIProperty(VisualElement container, SerializedProperty parentProperty, string propertyName, string label, StepSO step)
        {
            if (container == null || parentProperty == null || step == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            SerializedProperty property = parentProperty.FindPropertyRelative(propertyName);

            if (property == null)
            {
                return;
            }

            PropertyField propertyField = new PropertyField(property, label);
            propertyField.BindProperty(property);
            propertyField.TrackPropertyValue(property, changedProperty => DisplayStep(step));

            container.Add(propertyField);
        }

        /// <summary>
        /// Get the UI element type currently configured on a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private static EUIElementType GetUIElementType(StepSO step)
        {
            if (step == null || !(step.CompletionData is TutorialUICompletionData uiData))
            {
                return EUIElementType.None;
            }

            return uiData.ElementType;
        }

        #endregion

        #region Connection Inspectors

        /// <summary>
        /// Display a StepSO to GameObject binding connection
        /// </summary>
        /// <param name="connection"></param>
        public void DisplayBindingConnection(BindingConnection connection)
        {
            ClearPanel();

            if (connection == null || !connection.IsValid)
            {
                DisplayInvalidTarget("The selected binding connection is invalid.");

                return;
            }

            inspectorPanel.Add(CreateTitle("Binding Connection"));
            inspectorPanel.Add(CreateSubtitle($"{connection.Step.name} → {connection.TargetGameObject.name}"));

            VisualElement sourceContainer = CreateSectionContainer();

            sourceContainer.Add(CreateSectionLabel("Source StepSO"));
            sourceContainer.Add(CreateObjectButton(connection.Step));

            inspectorPanel.Add(sourceContainer);

            VisualElement targetContainer = CreateSectionContainer();

            targetContainer.Add(CreateSectionLabel("Target GameObject"));
            targetContainer.Add(CreateObjectButton(connection.TargetGameObject));

            inspectorPanel.Add(targetContainer);

            Label deletionHelp = CreateInformationLabel("Press Delete or Backspace to remove this binding.");
            deletionHelp.style.color = WarningTextColor;

            inspectorPanel.Add(deletionHelp);
        }

        /// <summary>
        /// Display a StepSO to StepSO sequence connection
        /// </summary>
        /// <param name="connection"></param>
        public void DisplaySequenceConnection(SequenceConnection connection)
        {
            ClearPanel();

            if (connection == null || !connection.IsValid)
            {
                DisplayInvalidTarget("The selected sequence connection is invalid.");

                return;
            }

            inspectorPanel.Add(CreateTitle("Sequence Connection"));
            inspectorPanel.Add(CreateSubtitle($"{connection.SourceStep.name} → {connection.TargetStep.name}"));

            VisualElement sequenceContainer = CreateSectionContainer();

            sequenceContainer.Add(CreateSectionLabel("StepSequenceSO"));
            sequenceContainer.Add(CreateObjectButton(connection.Sequence));

            inspectorPanel.Add(sequenceContainer);

            VisualElement sourceContainer = CreateSectionContainer();

            sourceContainer.Add(CreateSectionLabel("Source StepSO"));
            sourceContainer.Add(CreateObjectButton(connection.SourceStep));

            inspectorPanel.Add(sourceContainer);

            VisualElement targetContainer = CreateSectionContainer();

            targetContainer.Add(CreateSectionLabel("Target StepSO"));
            targetContainer.Add(CreateObjectButton(connection.TargetStep));

            inspectorPanel.Add(targetContainer);

            Label deletionHelp = CreateInformationLabel("Press Delete or Backspace to remove this sequence connection.");
            deletionHelp.style.color = WarningTextColor;

            inspectorPanel.Add(deletionHelp);
        }

        #endregion

        #region Unsupported Target

        /// <summary>
        /// Display a message for an unsupported Unity object
        /// </summary>
        /// <param name="target"></param>
        public void DisplayUnsupported(UnityObject target)
        {
            ClearPanel();

            if (target == null)
            {
                DisplayInvalidTarget("The selected object is missing.");

                return;
            }

            inspectorPanel.Add(CreateObjectButton(target));
            inspectorPanel.Add(CreateSubtitle(target.GetType().Name));
            inspectorPanel.Add(CreateInformationLabel("This object type is not supported by the tutorial inspector."));
        }

        /// <summary>
        /// Display an invalid selection message
        /// </summary>
        /// <param name="message"></param>
        private void DisplayInvalidTarget(string message)
        {
            Label errorLabel = CreateInformationLabel(message);

            errorLabel.style.color = ErrorTextColor;
            inspectorPanel.Add(errorLabel);
        }

        #endregion

        #region UI Factories

        /// <summary>
        /// Create a button selecting and pinging a Unity object
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static Button CreateObjectButton(UnityObject target)
        {
            string objectName = target != null ? target.name : "Missing Object";

            Button button = new Button(() =>
            {
                if (target == null)
                {
                    return;
                }

                Selection.activeObject = target;
                EditorGUIUtility.PingObject(target);
            })
            {
                text = objectName,
                tooltip = target != null ? $"Select {objectName}" : "Missing object"
            };

            button.style.height = 28f;
            button.style.marginBottom = 4f;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.SetEnabled(target != null);

            return button;
        }

        /// <summary>
        /// Create the main inspector title
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Label CreateTitle(string text)
        {
            Label title = new Label(text);

            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.whiteSpace = WhiteSpace.Normal;
            title.style.marginBottom = 4f;

            return title;
        }

        /// <summary>
        /// Create a secondary inspector title
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Label CreateSubtitle(string text)
        {
            Label subtitle = new Label(text);

            subtitle.style.whiteSpace = WhiteSpace.Normal;
            subtitle.style.color = SecondaryTextColor;
            subtitle.style.marginBottom = 10f;

            return subtitle;
        }

        /// <summary>
        /// Create a section title
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Label CreateSectionLabel(string text)
        {
            Label label = new Label(text);

            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 4f;

            return label;
        }

        /// <summary>
        /// Create an information label
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Label CreateInformationLabel(string text)
        {
            Label label = new Label(text);

            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = SecondaryTextColor;
            label.style.marginBottom = 8f;

            return label;
        }

        /// <summary>
        /// Create a visual section container
        /// </summary>
        /// <returns></returns>
        private static VisualElement CreateSectionContainer()
        {
            VisualElement container = new VisualElement();

            container.style.marginBottom = 8f;
            container.style.paddingLeft = 6f;
            container.style.paddingRight = 6f;
            container.style.paddingTop = 6f;
            container.style.paddingBottom = 6f;
            container.style.backgroundColor = ContainerBackgroundColor;

            return container;
        }

        #endregion

        #region Panel

        /// <summary>
        /// Clear the current inspector content
        /// </summary>
        private void ClearPanel()
        {
            inspectorPanel.Clear();
            selectedSerializedObject = null;
        }

        #endregion
    }
}