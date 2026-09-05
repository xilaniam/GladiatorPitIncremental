using UnityEngine;

[CreateAssetMenu(
    menuName = "SkillTree/Effects/Modify Ability"
)]
public class ModifyAbilityEffect : SkillEffect
{
    [SerializeField]
    private AbilityConfiguration ability;

    [SerializeField]
    private AbilityStat stat;

    [SerializeField]
    private StatOperation operation;

    [SerializeField]
    private float value;

    public override void Apply(SkillContext skillContext)
    {
        if (!skillContext.AbilityManager.TryGetAbility(ability, out IAbility runtimeAbility))
        {
            Debug.LogWarning($"Ability {ability.name} has not been unlocked.");
            return;
        }

        if (runtimeAbility is IModifiableAbility modifiable)
        {
            modifiable.Modify(stat,operation,value);
        }
    }

    public override string GetDescription()
    {
        return $"{ability.name}: {stat} {operation} {value}";
    }
}
