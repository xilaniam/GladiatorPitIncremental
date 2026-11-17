using UnityEngine;

public class DebugCheats : MonoBehaviour
{
    [Header("Money")]
    [SerializeField] bool infiniteMoney;
    [SerializeField] int infiniteBalance = 1000000000;
    [SerializeField] int coinsPerGrant = 10000;
    [SerializeField] KeyCode toggleInfiniteMoneyKey = KeyCode.F1;

    void Awake()
    {
        if (!Debug.isDebugBuild) enabled = false;
    }

    void OnEnable()
    {
        EventManager.OnBalanceUpdatedEvent += OnBalanceUpdated;
        TopUp();
    }

    void OnDisable()
    {
        EventManager.OnBalanceUpdatedEvent -= OnBalanceUpdated;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleInfiniteMoneyKey))
        {
            infiniteMoney = !infiniteMoney;
            Debug.Log($"[DebugCheats] Infinite money {(infiniteMoney ? "ON" : "OFF")}");
            TopUp();
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && enabled) TopUp();
    }

    void OnBalanceUpdated(int balance) => TopUp();

    void TopUp()
    {
        EconomyManager economy = EconomyManager.Instance;
        if (!infiniteMoney || economy == null) return;

        if (economy.Balance < infiniteBalance)
            economy.AddCoins(infiniteBalance - economy.Balance);
    }

    [ContextMenu("Grant Coins")]
    void GrantCoins()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.AddCoins(coinsPerGrant);
    }
}
