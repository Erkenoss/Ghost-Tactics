using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class OpenButton : MonoBehaviour
{
    #region Public Fields
    #endregion

    #region Private Fields

    /// <summary>
    /// GameObject to close
    /// </summary>
    [SerializeField]
    private GameObject toOpen = null;

    /// <summary>
    /// Close Button
    /// </summary>
    [SerializeField]
    private Button button = null;

    /// <summary>
    /// Action when we click on the button
    /// </summary>
    private UnityAction onClickAction = null;

    #endregion

    #region MonoBehaviour Callbacks

    private void OnEnable()
    {
        if (button != null && toOpen != null)
        {
            onClickAction = () => Execute();
            button.onClick.AddListener(onClickAction);
        }
    }

    private void OnDisable()
    {
        if (button != null && toOpen != null)
        {
            button.onClick.RemoveListener(onClickAction);
        }
    }

    #endregion

    #region Public Methods

    public void Execute()
    {
        if (toOpen != null)
        {
            toOpen.SetActive(true);
        }
    }

    #endregion

    #region Private Methods
    #endregion
}
