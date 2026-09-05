using UnityEngine;

[CreateAssetMenu(menuName = "Variables/Float Variable")]
public class FloatVariable : ScriptableObject
{
    public float DefaultValue;
    [System.NonSerialized] public float Value;

    // Careful: with "domain reload disabled" in Enter Play Mode Options,
    // OnEnable won't refire between play sessions — call ResetToDefault()
    // explicitly from a GameManager at run start instead of relying on this.
    void OnEnable() => Value = DefaultValue;

    public void ResetToDefault() => Value = DefaultValue;
}
