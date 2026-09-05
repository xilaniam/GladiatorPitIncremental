using UnityEngine;

[CreateAssetMenu(menuName = "Variables/Int Variable")]
public class IntVariable : ScriptableObject
{
    public int DefaultValue;
    [System.NonSerialized] public int Value;
    void OnEnable() => Value = DefaultValue;
    public void ResetToDefault() => Value = DefaultValue;
}