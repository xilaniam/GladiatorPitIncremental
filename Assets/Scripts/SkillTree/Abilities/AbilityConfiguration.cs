using UnityEngine;

public abstract class AbilityConfiguration : ScriptableObject
{
    public abstract IAbility CreateRuntime();
}
