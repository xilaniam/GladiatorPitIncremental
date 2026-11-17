using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class RicochetAbility : IAbility, IModifiableAbility
{
    private readonly RicochetConfig config;
    private int bounceCount;
    private float bounceDamagePercent;
    private float splinterDamagePercent;

    private SkillContext context;
    private readonly List<Enemy> candidates = new List<Enemy>();

    public RicochetAbility(RicochetConfig config)
    {
        this.config = config;
        bounceCount = config.BounceCount;
        bounceDamagePercent = config.BounceDamagePercent;
        splinterDamagePercent = config.SplinterDamagePercent;
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
        Vector3 origin = source.transform.position;

        LaunchBounce(origin, source, spearDamage * bounceDamagePercent, bounceCount);

        if (splinterDamagePercent > 0f)
            Launch(config.ShardProjectile, origin, PickRandomTarget(source), spearDamage * splinterDamagePercent, null);
    }

    private void LaunchBounce(Vector3 from, Enemy previous, float damage, int remaining)
    {
        if (remaining <= 0) return;

        Launch(config.BounceProjectile, from, PickRandomTarget(previous), damage,
            (landedAt, hit) => LaunchBounce(landedAt, hit != null ? hit : previous, damage, remaining - 1));
    }

    private void Launch(HomingProjectile prefab, Vector3 from, Enemy target, float damage, Action<Vector3, Enemy> then)
    {
        if (prefab == null || target == null) return;

        HomingProjectile projectile = Object.Instantiate(prefab);
        projectile.Launch(from, target, (landedAt, hit) =>
        {
            if (hit != null && !hit.IsDead) hit.TakeDamage(damage, hit.transform.position - from);
            then?.Invoke(landedAt, hit);
        });
    }

    private Enemy PickRandomTarget(Enemy exclude)
    {
        candidates.Clear();
        foreach (Enemy enemy in context.EnemyManager.AliveEnemies)
        {
            if (enemy != null && enemy != exclude && !enemy.IsDead) candidates.Add(enemy);
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    public void Modify(AbilityStat stat, StatOperation operation, float value)
    {
        switch (stat)
        {
            case AbilityStat.BounceCount:
                bounceCount = Mathf.RoundToInt(operation.Apply(bounceCount, value));
                break;

            case AbilityStat.Damage:
                bounceDamagePercent = operation.Apply(bounceDamagePercent, value);
                break;

            case AbilityStat.SplinterDamage:
                splinterDamagePercent = operation.Apply(splinterDamagePercent, value);
                break;
        }
    }
}
