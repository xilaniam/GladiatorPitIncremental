using System.Collections.Generic;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [SerializeField] int balance;
    [SerializeField] IntVariable additiveMultiplier;

    private readonly List<System.Func<int,int>> coinModifiers = new List<System.Func<int, int>>();

    public int Balance => balance;
    public static EconomyManager Instance { get; private set; }

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
        EventManager.OnCoinDroppedEvent += CollectCoin;
    }

    private void OnDestroy()
    {
        EventManager.OnCoinDroppedEvent -= CollectCoin;
    }

    void CollectCoin(int value)
    {
        int finalValue = value;
        foreach (var modifier in coinModifiers)
        {
            finalValue = modifier(value);
        }
        balance += (finalValue + additiveMultiplier.Value);
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    public void DeductAmount(int amount)
    {
        balance -= amount;
        EventManager.InvokeBalanceUpdatedEvent(balance);
    }

    public void RegisterCoinModifier(System.Func<int, int> modifier) => coinModifiers.Add(modifier);
    public void UnregisterCoinModifier(System.Func<int, int> modifier) => coinModifiers.Remove(modifier);
}
