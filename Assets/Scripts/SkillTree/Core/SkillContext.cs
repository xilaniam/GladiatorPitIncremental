using UnityEngine;

public class SkillContext 
{
    public SkillManager SkillManager { get; }
    public AbilityManager AbilityManager { get; }
   public EconomyManager EconomyManager { get; }
    public SkillContext(
        SkillManager skillManager,
        AbilityManager abilityManager,
        EconomyManager economyManager)
    {
        SkillManager = skillManager;
        AbilityManager = abilityManager;
        EconomyManager = economyManager;
    }
}
