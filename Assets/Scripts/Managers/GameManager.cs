using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] GladiatorManager gladiatorManager;
    [SerializeField] EnemyManager enemyManager;

    void Start()
    {
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeAllEvents();
    }

    void SubscribeToEvents()
    {
        RoundManager.Instance.RoundStartedEvent += StartRound;
        RoundManager.Instance.RoundEndedEvent += Cleanup;
    }

    void UnsubscribeAllEvents()
    {
        RoundManager.Instance.RoundStartedEvent -= StartRound;
        RoundManager.Instance.RoundEndedEvent -= Cleanup;
    }

    void StartRound()
    {
        gladiatorManager.SpawnGladiators();
        enemyManager.SpawnEnemies();
    }

    void Cleanup()
    {
        gladiatorManager.Cleanup();
        enemyManager.Cleanup();
    }
}
