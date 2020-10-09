using System.Collections;
using UnityEngine;

public class UpdateData : ScriptableObject {
    
    public event System.Action OnValuesUpdate;
    public bool AutoUpdate;

    #if UNITY_EDITOR

    protected virtual void OnValidate()
    {
        if (AutoUpdate)
        {
            UnityEditor.EditorApplication.update += NotifyOfUpdateValues;
        }
    }

    public void NotifyOfUpdateValues()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdateValues;
        if (OnValuesUpdate != null)
        {
            OnValuesUpdate();
        }
                
    }

    #endif

}
