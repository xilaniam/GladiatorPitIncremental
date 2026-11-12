using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class EconomyManager : GameEventListener
{
    [SerializeField] int balance;
    [SerializeField] IntVariable additiveMultiplier;
    [SerializeField] FloatVariable bonusPercentage;

    private readonly List<System.Func<int,int>> coinModifiers = new List<System.Func<int, int>>();
    private int roundCollection;

    public int Balance => balance;
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
        EventManager.OnCoinDroppedEvent += CollectCoin;
    }
    protected override void UnsubscribeEvents()
    {
        EventManager.RoundStartedEvent -= ResetRoundCollection;
        EventManager.RoundEndedEvent -= HandleRoundEndBalance;
        EventManager.OnCoinDroppedEvent -= CollectCoin;
    }
    #endregion

    void CollectCoin(int value)
    {
        int finalValue = value;
        foreach (var modifier in coinModifiers)
        {
            finalValue = modifier(finalValue);
        }
        finalValue += additiveMultiplier.Value;
        roundCollection += finalValue;
        balance += finalValue;
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    public void DeductAmount(int amount)
    {
        balance -= amount;
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    void ResetRoundCollection()
    {
        roundCollection = 0;
    }

    void HandleRoundEndBalance()
    {
        int additionalBonus = Mathf.CeilToInt((bonusPercentage.Value / 100) * roundCollection);
        balance += additionalBonus;
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    public void RegisterCoinModifier(System.Func<int, int> modifier) => coinModifiers.Add(modifier);
    public void UnregisterCoinModifier(System.Func<int, int> modifier) => coinModifiers.Remove(modifier);
}
