using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [SerializeField] int currency;
    int additiveMultiplier = 0;
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
        currency += (value + additiveMultiplier);
    }
}
