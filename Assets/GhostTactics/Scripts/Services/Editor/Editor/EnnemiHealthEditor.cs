using UnityEditor;
using UnityEngine;
using GhostTactics.Ennemi;

[CustomEditor(typeof(EnnemiHealth))]
public class EnnemiHealthEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnnemiHealth script = (EnnemiHealth)target;

        if (GUILayout.Button("Kill Enemy"))
        {
            script.EnnemiDie();
        }
    }
}