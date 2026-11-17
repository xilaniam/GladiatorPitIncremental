using UnityEngine;

public class BreedingSeasonAbility : IAbility, IModifiableAbility
{
    private float chance;
    private SkillContext context;

    public BreedingSeasonAbility(BreedingSeasonConfig config)
    {
        chance = config.Chance;
    }

    public void Enable(SkillContext context)
    {
        this.context = context;
        EventManager.OnEnemyDeadEvent += OnEnemyDead;
    }

    public void Disable()
    {
        EventManager.OnEnemyDeadEvent -= OnEnemyDead;
    }

    private void OnEnemyDead(Enemy enemy)
    {
        if (Random.value < chance)
            context.EnemyManager.TrySpawnExtra();
    }

    public void Modify(AbilityStat stat, StatOperation operation, float value)
    {
        if (stat != AbilityStat.Chance) return;
        chance = Mathf.Clamp01(operation.Apply(chance, value));
    }
}
