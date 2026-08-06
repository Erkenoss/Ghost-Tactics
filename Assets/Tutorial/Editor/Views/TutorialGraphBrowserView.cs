using System;
using System.Collections.Generic;
using Tutorial.Runtime.Persistence;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Display and filter TutorialGraphAsset assets stored in the project
    /// </summary>
    internal sealed class TutorialGraphBrowserView
    {
        #region Events

        public event Action<TutorialGraphAsset> OpenRequested = null;
        public event Action<TutorialGraphAsset> LocateRequested = null;
        public event Action RefreshRequested = null;
        public event Action BackRequested = null;

        #endregion

        #region Public Properties

        public VisualElement Root { get; } = null;

        #endregion

        #region Private Fields

        private readonly List<TutorialGraphAsset> graphs = new List<TutorialGraphAsset>();

        private readonly TextField searchField = null;
        private readonly ScrollView graphList = null;
        private readonly Label resultLabel = null;

        #endregion

        #region Constructor

        public TutorialGraphBrowserView()
        {
            Root = new VisualElement
            {
                name = "tutorial-graph-browser"
            };

            Root.style.flexGrow = 1f;
            Root.style.paddingLeft = 18f;
            Root.style.paddingRight = 18f;
            Root.style.paddingTop = 18f;
            Root.style.paddingBottom = 18f;
            Root.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            Label title = new Label("Tutorial Graph Browser");
            title.style.fontSize = 22f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 12f;

            VisualElement commandRow = new VisualElement();
            commandRow.style.flexDirection = FlexDirection.Row;
            commandRow.style.marginBottom = 10f;

            searchField = new TextField
            {
                name = "tutorial-graph-search-field",
                value = string.Empty,
                tooltip = "Filter tutorial graphs by name"
            };

            searchField.style.flexGrow = 1f;
            searchField.style.marginRight = 8f;
            searchField.RegisterValueChangedCallback(OnSearchChanged);

            Button refreshButton = new Button(OnRefreshClicked)
            {
                name = "tutorial-browser-refresh-button",
                text = "Refresh"
            };

            Button backButton = new Button(OnBackClicked)
            {
                name = "tutorial-browser-back-button",
                text = "Back"
            };

            refreshButton.style.width = 90f;
            backButton.style.width = 90f;
            backButton.style.marginLeft = 6f;

            commandRow.Add(searchField);
            commandRow.Add(refreshButton);
            commandRow.Add(backButton);

            resultLabel = new Label("0 graphs");
            resultLabel.style.color = new Color(0.65f, 0.65f, 0.65f);
            resultLabel.style.marginBottom = 8f;

            graphList = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "tutorial-graph-browser-list"
            };

            graphList.style.flexGrow = 1f;
            graphList.style.backgroundColor = new Color(0.17f, 0.17f, 0.17f);

            Root.Add(title);
            Root.Add(commandRow);
            Root.Add(resultLabel);
            Root.Add(graphList);

            Hide();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Replace the graphs currently displayed by the browser
        /// </summary>
        /// <param name="availableGraphs"></param>
        public void SetGraphs(IReadOnlyList<TutorialGraphAsset> availableGraphs)
        {
            graphs.Clear();

            if (availableGraphs != null)
            {
                foreach (TutorialGraphAsset graph in availableGraphs)
                {
                    if (graph != null)
                    {
                        graphs.Add(graph);
                    }
                }
            }

            RebuildGraphList();
        }

        /// <summary>
        /// Show the graph browser
        /// </summary>
        public void Show()
        {
            Root.style.display = DisplayStyle.Flex;

            searchField.schedule.Execute(() =>
            {
                searchField.Focus();
            });
        }

        /// <summary>
        /// Hide the graph browser
        /// </summary>
        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
        }

        #endregion

        #region List

        /// <summary>
        /// Rebuild visible graph rows using the current search value
        /// </summary>
        private void RebuildGraphList()
        {
            graphList.Clear();

            string searchValue = searchField.value?.Trim() ?? string.Empty;
            int visibleGraphCount = 0;

            foreach (TutorialGraphAsset graph in graphs)
            {
                if (!MatchesSearch(graph, searchValue))
                {
                    continue;
                }

                graphList.Add(CreateGraphRow(graph));
                visibleGraphCount++;
            }

            resultLabel.text = visibleGraphCount == 1 ? "1 graph" : $"{visibleGraphCount} graphs";

            if (visibleGraphCount == 0)
            {
                Label emptyLabel = new Label("No TutorialGraphAsset found.");
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.marginTop = 24f;

                graphList.Add(emptyLabel);
            }
        }

        /// <summary>
        /// Create one graph browser row
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        private VisualElement CreateGraphRow(TutorialGraphAsset graph)
        {
            VisualElement row = new VisualElement
            {
                name = "tutorial-graph-row"
            };

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 42f;
            row.style.paddingLeft = 8f;
            row.style.paddingRight = 8f;
            row.style.marginBottom = 2f;
            row.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            VisualElement information = new VisualElement();
            information.style.flexGrow = 1f;
            information.style.minWidth = 0f;

            Label nameLabel = new Label(graph.name);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            Label pathLabel = new Label(AssetDatabase.GetAssetPath(graph));
            pathLabel.style.fontSize = 10f;
            pathLabel.style.color = new Color(0.58f, 0.58f, 0.58f);
            pathLabel.style.whiteSpace = WhiteSpace.NoWrap;

            information.Add(nameLabel);
            information.Add(pathLabel);

            Button openButton = new Button(() => OpenRequested?.Invoke(graph))
            {
                text = "Open"
            };

            Button locateButton = new Button(() => LocateRequested?.Invoke(graph))
            {
                text = "Locate"
            };

            openButton.style.width = 70f;
            locateButton.style.width = 70f;
            locateButton.style.marginLeft = 5f;

            row.Add(information);
            row.Add(openButton);
            row.Add(locateButton);

            return row;
        }

        private static bool MatchesSearch(TutorialGraphAsset graph, string searchValue)
        {
            if (graph == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                return true;
            }

            return graph.name.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region Callbacks

        private void OnSearchChanged(ChangeEvent<string> changeEvent)
        {
            RebuildGraphList();
        }

        private void OnRefreshClicked()
        {
            RefreshRequested?.Invoke();
        }

        private void OnBackClicked()
        {
            BackRequested?.Invoke();
        }

        #endregion
    }
}