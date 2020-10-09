using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(display))]
public class ButtonGenerato : Editor
{
    public override void OnInspectorGUI()
    {
        display d = (display)target;

        if (DrawDefaultInspector())
        {
            if(d.autoUpdate)
            {
                d.IniGenerate();
            }
        }

        if(GUILayout.Button("Generuj"))
        {
            d.IniGenerate();
        }
    }
}