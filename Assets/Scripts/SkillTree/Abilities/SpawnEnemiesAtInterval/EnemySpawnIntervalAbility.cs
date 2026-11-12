using System.Collections;
using UnityEngine;

public class EnemySpawnIntervalAbility : IAbility, IModifiableAbility
{
    private float spawnInterval;
    private SkillContext context;

    Coroutine spawnCoroutine;
    public EnemySpawnIntervalAbility(EnemySpawnIntervalConfig config)
    {
        spawnInterval = config.SpawnInterval;
    }

    public void Enable(SkillContext context)
    {
        this.context = context;
        EventManager.RoundStartedEvent += StartSpawningEnemies;
        EventManager.RoundEndedEvent += StopSpawningEnemies;
        StopSpawningEnemies();
    }

    public void Disable()
    {
        EventManager.RoundStartedEvent -= StartSpawningEnemies;
        EventManager.RoundEndedEvent -= StopSpawningEnemies;
    }

    public void Modify(AbilityStat stat, StatOperation operation, float value)
    {
        if (stat != AbilityStat.Duration) return;
        spawnInterval = operation switch
        {
            StatOperation.Add => spawnInterval + value,
            StatOperation.Multiply => spawnInterval * value,
            StatOperation.Set => value,
            _ => spawnInterval
        };
    }

    void StartSpawningEnemies()
    {
        spawnCoroutine = context.AbilityManager.StartCoroutine(SpawnCoroutine());
    }

    void StopSpawningEnemies()
    {
        if (spawnCoroutine != null)
        {
            context.AbilityManager.StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            context.EnemyManager.SpawnEnemy();
        }
    }
}
