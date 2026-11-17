using SMPro.SkillTree;
using UnityEngine;

public class HuntersFrenzyFeature : SkillFeature
{
    [SerializeField] EconomyManager economy;

    [Header("Upgradable")]
    [SerializeField] StatRef killThreshold;
    [SerializeField] StatRef coinBonus;

    [Header("Boosted While Active")]
    [SerializeField] StatRef attackInterval;
    [SerializeField] StatRef damageMultiplier;

    [Header("While Active")]
    [SerializeField] float duration = 5f;
    [SerializeField, Min(0.01f)] float attackSpeedFactor = 2f;
    [SerializeField] float damageFactor = 1.5f;

    int killsThisRound;
    float activeUntil;

    public bool IsActive => Time.time < activeUntil;

    protected override void OnUnlocked()
    {
        EventManager.OnEnemyDeadEvent += OnEnemyDead;
        EventManager.RoundStartedEvent += ResetKills;
        EventManager.RoundEndedEvent += EndFrenzy;
        economy.RegisterCoinModifier(ApplyCoinBonus);
    }

    protected override void OnLocked()
    {
        EventManager.OnEnemyDeadEvent -= OnEnemyDead;
        EventManager.RoundStartedEvent -= ResetKills;
        EventManager.RoundEndedEvent -= EndFrenzy;
        if (economy != null) economy.UnregisterCoinModifier(ApplyCoinBonus);
        EndFrenzy();
    }

    void ResetKills() => killsThisRound = 0;

    void OnEnemyDead(Enemy enemy)
    {
        killsThisRound++;
        if (killsThisRound % Mathf.Max(1, killThreshold.IntValue) == 0) StartFrenzy();
    }

    void StartFrenzy()
    {
        RemoveBoosts();
        attackInterval.AddTemporary(ModifierKind.Multiply, 1f / attackSpeedFactor, duration, this);
        damageMultiplier.AddTemporary(ModifierKind.Multiply, damageFactor, duration, this);
        activeUntil = Time.time + duration;
    }

    void EndFrenzy()
    {
        RemoveBoosts();
        activeUntil = 0f;
    }

    void RemoveBoosts()
    {
        attackInterval.RemoveModifiers(this);
        damageMultiplier.RemoveModifiers(this);
    }

    float ApplyCoinBonus(float coins) => IsActive ? coins * (1f + coinBonus.Value) : coins;
}
