using System.Collections.Generic;
using SMPro.SkillTree;
using UnityEngine;

public class ChainLightningFeature : SkillFeature
{
    [SerializeField] EnemyManager enemyManager;
    [SerializeField] StatRef chance;
    [SerializeField] StatRef jumps;
    [SerializeField] StatRef damagePercent;

    readonly List<Enemy> targets = new();

    protected override void OnUnlocked() => EventManager.OnSpearHitEvent += OnSpearHit;

    protected override void OnLocked() => EventManager.OnSpearHitEvent -= OnSpearHit;

    void OnSpearHit(Enemy source, float spearDamage)
    {
        if (Random.value >= chance.Value) return;

        Vector3 origin = source.transform.position;
        targets.Clear();
        foreach (Enemy enemy in enemyManager.AliveEnemies)
        {
            if (enemy != null && enemy != source && !enemy.IsDead) targets.Add(enemy);
        }
        targets.Sort((a, b) =>
            (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));

        float jumpDamage = spearDamage * damagePercent.Value;
        int count = Mathf.Min(jumps.IntValue, targets.Count);
        for (int i = 0; i < count; i++) targets[i].TakeDamage(jumpDamage);
    }
}
