using UnityEngine;

public class SkillContext 
{
    public SkillManager SkillManager { get; }
    public AbilityManager AbilityManager { get; }
    public EconomyManager EconomyManager { get; }
    public EnemyManager EnemyManager { get; }
    public SkillContext(
        SkillManager skillManager,
        AbilityManager abilityManager,
        EconomyManager economyManager,
        EnemyManager enemyManager)
    {
        SkillManager = skillManager;
        AbilityManager = abilityManager;
        EconomyManager = economyManager;
        EnemyManager = enemyManager;
    }
}
