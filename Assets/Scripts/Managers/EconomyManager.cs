using System;
using System.Collections.Generic;
using UnityEngine;

public class EconomyManager : GameEventListener
{
    [SerializeField] int balance;
    [SerializeField] FloatVariable coinMultiplier;
    [SerializeField] FloatVariable coinBonusPerTier;
    [SerializeField] IntVariable unlockedTiers;
    [SerializeField] FloatVariable bonusPercentage;

    private readonly List<Func<float, float>> coinModifiers = new List<Func<float, float>>();
    private int roundCollection;
    private float coinRemainder;

    public int Balance => balance;
    public int LastRoundBonus { get; private set; }
    public static EconomyManager Instance { get; private set; }

    #region ------Initialization And Subscription -------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        SubscribeEvents();
    }
    private void OnDestroy()
    {
       UnsubscribeEvents();
    }
    protected override void SubscribeEvents()
    {
        EventManager.RoundStartedEvent += ResetRoundCollection;
        EventManager.RoundEndedEvent += HandleRoundEndBalance;
        EventManager.OnEnemyDeadEvent += CollectCoins;
    }
    protected override void UnsubscribeEvents()
    {
        EventManager.RoundStartedEvent -= ResetRoundCollection;
        EventManager.RoundEndedEvent -= HandleRoundEndBalance;
        EventManager.OnEnemyDeadEvent -= CollectCoins;
    }
    #endregion

    void CollectCoins(Enemy enemy)
    {
        float coins = enemy.CoinValue * coinMultiplier.Value * (1f + coinBonusPerTier.Value * unlockedTiers.Value);
        foreach (var modifier in coinModifiers)
        {
            coins = modifier(coins);
        }

        coinRemainder += coins;
        int earned = Mathf.FloorToInt(coinRemainder);
        coinRemainder -= earned;

        roundCollection += earned;
        balance += earned;
        EventManager.InvokeCoinsEarnedEvent(enemy, earned);
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    public void DeductAmount(int amount)
    {
        balance -= amount;
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    public void AddCoins(int amount)
    {
        balance += amount;
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    void ResetRoundCollection()
    {
        roundCollection = 0;
    }

    void HandleRoundEndBalance()
    {
        int additionalBonus = Mathf.CeilToInt((bonusPercentage.Value / 100) * roundCollection);
        LastRoundBonus = additionalBonus;
        balance += additionalBonus;
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    public void RegisterCoinModifier(Func<float, float> modifier) => coinModifiers.Add(modifier);
    public void UnregisterCoinModifier(Func<float, float> modifier) => coinModifiers.Remove(modifier);
}
