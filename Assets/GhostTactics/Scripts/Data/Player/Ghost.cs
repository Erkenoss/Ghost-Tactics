using System.Collections.Generic;
using GhostTactics.Core;
using GhostTactics.Data;

public class Ghost
{
    #region Public Fields
    public List<AbilityData> ActionsGhost { get { return actionsGhost; } }
    public List<string> AbilitiesName { get { return abilitiesName; } }

    #endregion

    #region Private Fields

    /// <summary>
    /// List of actions that the ghost can perform
    /// </summary>
    private List<AbilityData> actionsGhost = new List<AbilityData>();

    /// <summary>
    /// Name of all ability in actionsGhost. Use to load and save the ghost's actions
    /// </summary>
    private List<string> abilitiesName = new List<string>();

    #endregion

    #region MonoBehaviour Callbacks
    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a list of actions to the ghost's action list
    /// </summary>
    /// <param name="actions"></param>
    public void AddAction(List<AbilityData> actions)
    {
        if (actions == null || actions.Count == 0)
        {
            return;
        }

        actionsGhost.Clear();
        abilitiesName.Clear();

        actionsGhost.AddRange(actions);
        abilitiesName.AddRange(actions.ConvertAll(action => action.Ability.ToString()));
    }

    /// <summary>
    /// Remove the Ability given as argument
    /// </summary>
    /// <param name="data"></param>
    public void RemoveAction(AbilityData data)
    {
        if (data == null || actionsGhost == null || actionsGhost.Count == 0 || abilitiesName == null || abilitiesName.Count == 0)
        { 
            return; 
        }

        actionsGhost.Remove(data);
        abilitiesName.Remove(data.Ability.ToString());
    }

    /// <summary>
    /// Gets the data of a specific ability by its name
    /// </summary>
    /// <param name="abilityName"></param>
    public void GetAbilityData(List<string> abilityName)
    {
        if (abilityName == null || abilityName.Count == 0)
        {
            return;
        }

        foreach (string ability in abilityName)
        {
            AbilityData data = ActionManager.Instance.GetAbilityByName(ability);
            
            if (data == null)
            {
                return;
            }
            
            actionsGhost.Add(data);
        }
    }

    /// <summary>
    /// Clear the list of actions name and ability
    /// </summary>
    public void ClearActionsList()
    {
        actionsGhost.Clear();
        abilitiesName.Clear();
    }

    #endregion

    #region Private Methods
    #endregion
}
