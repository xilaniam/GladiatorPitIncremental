using System.Collections.Generic;
using UnityEngine;

public class ChainLightningAbility : IAbility, IModifiableAbility
{
    private float damagePercent;
    private float chance;
    private int chainCount;

    private SkillContext context;
    private readonly List<Enemy> targets = new List<Enemy>();

    public ChainLightningAbility(ChainLightningConfig config)
    {
        damagePercent = config.DamagePercent;
        chance = config.Chance;
        chainCount = config.ChainCount;
    }

    public void Enable(SkillContext context)
    {
        this.context = context;
        EventManager.OnSpearHitEvent += OnSpearHit;
    }

    public void Disable()
    {
        EventManager.OnSpearHitEvent -= OnSpearHit;
    }

    private void OnSpearHit(Enemy source, float spearDamage)
    {
        if (Random.value >= chance) return;

        Vector3 origin = source.transform.position;
        targets.Clear();
        foreach (Enemy enemy in context.EnemyManager.AliveEnemies)
        {
            if (enemy != source) targets.Add(enemy);
        }
        targets.Sort((a, b) =>
            (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));

        float jumpDamage = spearDamage * damagePercent;
        int jumps = Mathf.Min(chainCount, targets.Count);
        for (int i = 0; i < jumps; i++)
        {
            targets[i].TakeDamage(jumpDamage);
        }
    }

    public void Modify(AbilityStat stat, StatOperation operation, float value)
    {
        switch (stat)
        {
            case AbilityStat.Damage:
                damagePercent = operation.Apply(damagePercent, value);
                break;

            case AbilityStat.Chance:
                chance = Mathf.Clamp01(operation.Apply(chance, value));
                break;

            case AbilityStat.ChainCount:
                chainCount = Mathf.RoundToInt(operation.Apply(chainCount, value));
                break;
        }
    }
}
