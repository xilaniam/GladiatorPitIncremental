using System;
using System.Collections.Generic;
using SMPro.SkillTree;
using UnityEngine;

public class EconomyManager : GameEventListener, ICurrencyWallet
{
    [SerializeField] int balance;
    [SerializeField] EnemyManager enemyManager;
    [SerializeField] StatRef coinMultiplier;
    [SerializeField] StatRef coinBonusPerTier;
    [SerializeField] StatRef bonusPercentage;

    private readonly List<Func<float, float>> coinModifiers = new List<Func<float, float>>();
    private int roundCollection;
    private float coinRemainder;

    public int Balance => balance;
    public int LastRoundBonus { get; private set; }
    public static EconomyManager Instance { get; private set; }

    public event Action<string> BalanceChanged;

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
        float coins = enemy.CoinValue * coinMultiplier.Value * (1f + coinBonusPerTier.Value * enemyManager.UnlockedTiers);
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
        NotifyBalanceChanged();
    }

    public void DeductAmount(int amount)
    {
        balance -= amount;
        NotifyBalanceChanged();
    }

    public void AddCoins(int amount)
    {
        balance += amount;
        NotifyBalanceChanged();
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
        NotifyBalanceChanged();
    }

    public bool CanAfford(string currencyId, long amount) => balance >= amount;

    public bool TrySpend(string currencyId, long amount)
    {
        if (amount < 0 || amount > balance) return false;
        DeductAmount((int)amount);
        return true;
    }

    public void Add(string currencyId, long amount) => AddCoins((int)Math.Min(amount, int.MaxValue - (long)balance));

    void NotifyBalanceChanged()
    {
        EventManager.InvokeBalanceUpdatedEvent(balance);
        BalanceChanged?.Invoke(CurrencyIds.Default);
    }

    public void RegisterCoinModifier(Func<float, float> modifier) => coinModifiers.Add(modifier);
    public void UnregisterCoinModifier(Func<float, float> modifier) => coinModifiers.Remove(modifier);
}
