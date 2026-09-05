using System.Collections.Generic;
using UnityEngine;

public class ChainLightningAbility : IAbility,IModifiableAbility
{
    private readonly ChainLightningConfig definition;

    private float damage;
    private float chance;
    private int chainCount;

    private SkillContext context;
    static readonly Collider[] hitBuffer = new Collider[10];
    readonly HashSet<Enemy> currentChaining = new HashSet<Enemy>();

    public ChainLightningAbility(ChainLightningConfig config)
    {
        this.definition = config;

        damage = config.Damage;
        chance = config.Chance;
        chainCount = config.ChainCount;
        
    }

    public void Enable(SkillContext context)
    {
        this.context = context;
        EventManager.OnEnemyDamagedEvent += OnEnemyDamaged;
    }

    public void Disable()
    {
        EventManager.OnEnemyDamagedEvent -= OnEnemyDamaged;
    }

    private void OnEnemyDamaged(Enemy enemy)
    {
        if(currentChaining.Contains(enemy)) return;
        TriggerChain(enemy);
    }

    private void TriggerChain(Enemy source)
    {
        if(Random.value > chance) return;

        int hitCount = Physics.OverlapSphereNonAlloc(
            source.transform.position,
            5f,
            hitBuffer,
            LayerMask.GetMask("Enemy")
        );
        int chained = 0;

        for( int i = 0; i< hitCount &&  chained < chainCount; i++ )
        {
            Enemy target = hitBuffer[i].GetComponent<Enemy>();
            if (target == null || target == source || currentChaining.Contains(target)) continue;
            currentChaining.Add(target);
            target.TakeDamage(damage);
            currentChaining.Remove(target);
            chained++;
        }
    }

    public void Modify(AbilityStat stat, StatOperation operation, float value)
    {
        switch (stat)
        {
            case AbilityStat.Damage:
                damage = ApplyOperation( damage, value, operation);
                break;

            case AbilityStat.Chance:
                chance = ApplyOperation( chance, value,operation );
                chance = Mathf.Clamp01(chance);
                break;

            case AbilityStat.ChainCount:
                chainCount = Mathf.RoundToInt(ApplyOperation(chainCount,value,operation));
                break;
        }
    }

    private float ApplyOperation( float current,float value,StatOperation operation)
    {
        return operation switch
        {
            StatOperation.Add => current + value,
            StatOperation.Multiply => current * value,
            StatOperation.Set => value,
            _ => current
        };
    }
}
