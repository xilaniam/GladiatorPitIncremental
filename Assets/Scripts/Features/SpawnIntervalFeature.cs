using System.Collections;
using SMPro.SkillTree;
using UnityEngine;

public class SpawnIntervalFeature : SkillFeature
{
    [SerializeField] EnemyManager enemyManager;
    [SerializeField] StatRef seconds;

    Coroutine spawnRoutine;

    protected override void OnUnlocked()
    {
        EventManager.RoundStartedEvent += StartSpawning;
        EventManager.RoundEndedEvent += StopSpawning;
        if (RoundManager.Instance != null && RoundManager.Instance.IsRoundActive) StartSpawning();
    }

    protected override void OnLocked()
    {
        EventManager.RoundStartedEvent -= StartSpawning;
        EventManager.RoundEndedEvent -= StopSpawning;
        StopSpawning();
    }

    void StartSpawning()
    {
        StopSpawning();
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    void StopSpawning()
    {
        if (spawnRoutine == null) return;
        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(seconds.Value);
            enemyManager.TrySpawnExtra();
        }
    }
}
