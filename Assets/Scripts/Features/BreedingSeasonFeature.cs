using SMPro.SkillTree;
using UnityEngine;

public class BreedingSeasonFeature : SkillFeature
{
    [SerializeField] EnemyManager enemyManager;
    [SerializeField] StatRef chance;

    protected override void OnUnlocked() => EventManager.OnEnemyDeadEvent += OnEnemyDead;

    protected override void OnLocked() => EventManager.OnEnemyDeadEvent -= OnEnemyDead;

    void OnEnemyDead(Enemy enemy)
    {
        if (Random.value < chance.Value) enemyManager.TrySpawnExtra();
    }
}
