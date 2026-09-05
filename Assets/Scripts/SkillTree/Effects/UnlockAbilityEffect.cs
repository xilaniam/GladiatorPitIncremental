using UnityEngine;

[CreateAssetMenu(fileName = "UnlockAbilityEffect", menuName = "SkillTree/Effects/UnlockAbilityEffect")]
public class UnlockAbilityEffect : SkillEffect
{
    [SerializeField] AbilityConfiguration ability;
    public override void Apply(SkillContext skillContext)
    {
        skillContext.AbilityManager.UnlockAbility(ability,skillContext);
    }
    public override string GetDescription()
    {
        return $"Unlock {ability.name}";
    }
}
