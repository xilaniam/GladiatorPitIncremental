using System.Collections.Generic;
using UnityEngine;

public class InstantKillAbility : IAbility, IModifiableAbility
{
    private float chance;
    private readonly HashSet<Enemy> processing = new HashSet<Enemy>();
    private SkillContext context;

    public InstantKillAbility(InstantKillConfig config) => chance = config.Chance;

    public void Enable(SkillContext context) => EventManager.OnEnemyDamagedEvent += OnEnemyDamaged;
    public void Disable() => EventManager.OnEnemyDamagedEvent -= OnEnemyDamaged;

    private void OnEnemyDamaged(Enemy enemy)
    {
        if (processing.Contains(enemy)) return;
        if (Random.value > chance) return;

        processing.Add(enemy);
        enemy.TakeDamage(float.MaxValue); // swap for enemy.Kill() if you have a dedicated method
        processing.Remove(enemy);
    }

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