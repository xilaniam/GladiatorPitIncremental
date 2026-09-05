using UnityEngine;

public class DoubleCoinsOnKillAbility : IAbility, IModifiableAbility
{
    private float chance;
    private SkillContext context;

    public DoubleCoinsOnKillAbility(DoubleCoinsOnKillConfig config)
    { 
        chance = config.Chance; 
    }

    public void Enable(SkillContext context) 
    { 
        this.context = context;
        context.EconomyManager.RegisterCoinModifier(ApplyDoubleChance); 
    }
    public void Disable() => context.EconomyManager.UnregisterCoinModifier(ApplyDoubleChance);

    private int ApplyDoubleChance(int value) => Random.value <= chance ? value * 2 : value;

    public void Modify(AbilityStat stat, StatOperation operation, float value)
    {
        if (stat != AbilityStat.Chance) return;
        chance = operation switch
        {
            StatOperation.Add => chance + value,
            StatOperation.Multiply => chance * value,
            StatOperation.Set => value,
            _ => chance
        };
        chance = Mathf.Clamp01(chance);
    }
}