using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnlockTracker : GameEventListener
{
    [SerializeField] EnemyManager enemyManager;
    [SerializeField] List<EnemyUnlockRule> rules = new List<EnemyUnlockRule>();

    private readonly Dictionary<AnimalType, int> kills = new Dictionary<AnimalType, int>();

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
    }

    protected override void UnsubscribeEvents()
    {
        EventManager.OnEnemyDeadEvent -= OnEnemyDead;
    }

    void OnEnemyDead(Enemy enemy)
    {
        kills[enemy.AnimalType] = GetKills(enemy.AnimalType) + 1;
        CheckRules(enemy.AnimalType);
    }

    void CheckRules(AnimalType killedType)
    {
        foreach (EnemyUnlockRule rule in rules)
        {
            if (rule.killType != killedType) continue;
            if (enemyManager.IsUnlocked(rule.unlocks)) continue;

            if (GetKills(killedType) >= rule.killsRequired)
            {
                enemyManager.UnlockEnemy(rule.unlocks);
            }
        }
    }
}

[Serializable]
public class EnemyUnlockRule
{
    public AnimalType killType; 
    public int killsRequired = 10;   
    public AnimalType unlocks;       
}