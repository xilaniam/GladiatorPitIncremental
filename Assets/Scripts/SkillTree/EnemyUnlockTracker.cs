using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnlockTracker : GameEventListener
{
    [SerializeField] EnemyManager enemyManager;
    [SerializeField] List<EnemyUnlockRule> rules = new List<EnemyUnlockRule>();

    private readonly Dictionary<AnimalType, int> kills = new Dictionary<AnimalType, int>();
    private readonly List<AnimalType> unlockedThisRound = new List<AnimalType>();

    public IReadOnlyList<AnimalType> UnlockedThisRound => unlockedThisRound;
    public int GetKills(AnimalType type) => kills.TryGetValue(type, out int count) ? count : 0;

    private void Start()
    {
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    protected override void SubscribeEvents()
    {
        EventManager.OnEnemyDeadEvent += OnEnemyDead;
        EventManager.RoundEndedEvent += ApplyUnlocks;
    }

    protected override void UnsubscribeEvents()
    {
        EventManager.OnEnemyDeadEvent -= OnEnemyDead;
        EventManager.RoundEndedEvent -= ApplyUnlocks;
    }

    void OnEnemyDead(Enemy enemy)
    {
        kills[enemy.AnimalType] = GetKills(enemy.AnimalType) + 1;
    }

    void ApplyUnlocks()
    {
        unlockedThisRound.Clear();
        foreach (EnemyUnlockRule rule in rules)
        {
            if (enemyManager.IsUnlocked(rule.unlocks)) continue;
            if (GetKills(rule.killType) < rule.killsRequired) continue;

            enemyManager.UnlockEnemy(rule.unlocks);
            unlockedThisRound.Add(rule.unlocks);
        }
    }

    public bool TryGetNextUnlock(out EnemyUnlockRule nextRule)
    {
        foreach (EnemyUnlockRule rule in rules)
        {
            if (enemyManager.IsUnlocked(rule.unlocks)) continue;
            if (!enemyManager.IsUnlocked(rule.killType)) continue;

            nextRule = rule;
            return true;
        }

        nextRule = null;
        return false;
    }
}

[Serializable]
public class EnemyUnlockRule
{
    public AnimalType killType;
    public int killsRequired = 10;
    public AnimalType unlocks;
}
