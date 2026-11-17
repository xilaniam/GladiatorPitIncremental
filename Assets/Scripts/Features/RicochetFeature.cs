using System;
using System.Collections.Generic;
using SMPro.SkillTree;
using UnityEngine;
using Random = UnityEngine.Random;

public class RicochetFeature : SkillFeature
{
    [SerializeField] EnemyManager enemyManager;

    [Header("Stats")]
    [SerializeField] StatRef bounces;
    [SerializeField] StatRef damagePercent;
    [SerializeField] StatRef splinterPercent;

    [Header("Visuals")]
    [SerializeField] HomingProjectile bounceProjectile;
    [SerializeField] HomingProjectile shardProjectile;

    readonly List<Enemy> candidates = new();

    protected override void OnUnlocked() => EventManager.OnSpearHitEvent += OnSpearHit;

    protected override void OnLocked() => EventManager.OnSpearHitEvent -= OnSpearHit;

    void OnSpearHit(Enemy source, float spearDamage)
    {
        Vector3 origin = source.transform.position;

        LaunchBounce(origin, source, spearDamage * damagePercent.Value, bounces.IntValue);

        if (splinterPercent.Value > 0f)
            Launch(shardProjectile, origin, PickRandomTarget(source), spearDamage * splinterPercent.Value, null);
    }

    void LaunchBounce(Vector3 from, Enemy previous, float damage, int remaining)
    {
        if (remaining <= 0) return;

        Launch(bounceProjectile, from, PickRandomTarget(previous), damage,
            (landedAt, hit) => LaunchBounce(landedAt, hit != null ? hit : previous, damage, remaining - 1));
    }

    void Launch(HomingProjectile prefab, Vector3 from, Enemy target, float damage, Action<Vector3, Enemy> then)
    {
        if (prefab == null || target == null || !IsUnlocked) return;

        HomingProjectile projectile = Instantiate(prefab);
        projectile.Launch(from, target, (landedAt, hit) =>
        {
            if (hit != null && !hit.IsDead) hit.TakeDamage(damage, hit.transform.position - from);
            then?.Invoke(landedAt, hit);
        });
    }

    Enemy PickRandomTarget(Enemy exclude)
    {
        candidates.Clear();
        foreach (Enemy enemy in enemyManager.AliveEnemies)
        {
            if (enemy != null && enemy != exclude && !enemy.IsDead) candidates.Add(enemy);
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }
}
