using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    private readonly Dictionary<AbilityConfiguration,IAbility> activeAbilities = new();

    public void UnlockAbility(AbilityConfiguration definition , SkillContext context)
    {
        if (definition == null) return;

        if (activeAbilities.ContainsKey(definition)) return;

        IAbility ability = definition.CreateRuntime();

        activeAbilities.Add(definition,ability);

        ability.Enable(context);
    }

    public bool TryGetAbility(AbilityConfiguration definition,out IAbility ability)
    {
        return activeAbilities.TryGetValue(
            definition,
            out ability
        );
    }

    private void OnDestroy()
    {
        foreach (var ability in activeAbilities.Values)
        {
            ability.Disable();
        }
        activeAbilities.Clear();
    }
}
