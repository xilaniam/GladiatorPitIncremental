using System.Collections;
using UnityEngine;

public class HuntersFrenzyAbility : IAbility, IModifiableAbility
{
    private readonly HuntersFrenzyConfig config;
    private int killsToTrigger;
    private float coinBonus;

    private SkillContext context;
    private int killsThisRound;
    private Coroutine frenzyRoutine;
    private float savedAttackInterval;
    private float savedDamageMultiplier;

    private bool IsActive => frenzyRoutine != null;

    public HuntersFrenzyAbility(HuntersFrenzyConfig config)
    {
        this.config = config;
        killsToTrigger = config.KillsToTrigger;
        coinBonus = config.CoinBonus;
    }

    public void Enable(SkillContext context)
    {
        this.context = context;
        EventManager.OnEnemyDeadEvent += OnEnemyDead;
        EventManager.RoundStartedEvent += ResetKills;
        EventManager.RoundEndedEvent += EndFrenzy;
        context.EconomyManager.RegisterCoinModifier(ApplyCoinBonus);
    }

    public void Disable()
    {
        EventManager.OnEnemyDeadEvent -= OnEnemyDead;
        EventManager.RoundStartedEvent -= ResetKills;
        EventManager.RoundEndedEvent -= EndFrenzy;
        context.EconomyManager.UnregisterCoinModifier(ApplyCoinBonus);
        EndFrenzy();
    }

    private void ResetKills() => killsThisRound = 0;

    private void OnEnemyDead(Enemy enemy)
    {
        killsThisRound++;
        if (killsThisRound % killsToTrigger == 0)
            StartFrenzy();
    }

    private void StartFrenzy()
    {
        if (IsActive)
        {
            context.AbilityManager.StopCoroutine(frenzyRoutine);
        }
        else
        {
            savedAttackInterval = config.AttackInterval.Value;
            savedDamageMultiplier = config.SpearDamageMultiplier.Value;
            config.AttackInterval.Value /= config.AttackSpeedMultiplier;
            config.SpearDamageMultiplier.Value *= config.DamageMultiplier;
        }

        frenzyRoutine = context.AbilityManager.StartCoroutine(FrenzyTimer());
    }

    private IEnumerator FrenzyTimer()
    {
        yield return new WaitForSeconds(config.Duration);
        EndFrenzy();
    }

    private void EndFrenzy()
    {
        if (!IsActive) return;

        if (context.AbilityManager != null)
            context.AbilityManager.StopCoroutine(frenzyRoutine);
        frenzyRoutine = null;

        config.AttackInterval.Value = savedAttackInterval;
        config.SpearDamageMultiplier.Value = savedDamageMultiplier;
    }

    private float ApplyCoinBonus(float coins) => IsActive ? coins * (1f + coinBonus) : coins;

    public void Modify(AbilityStat stat, StatOperation operation, float value)
    {
        switch (stat)
        {
            case AbilityStat.KillThreshold:
                killsToTrigger = Mathf.Max(config.MinKillsToTrigger, Mathf.RoundToInt(operation.Apply(killsToTrigger, value)));
                break;

            case AbilityStat.CoinBonus:
                coinBonus = operation.Apply(coinBonus, value);
                break;
        }
    }
}
