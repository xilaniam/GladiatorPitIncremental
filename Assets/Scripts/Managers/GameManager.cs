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
        RoundManager.Instance.RoundStartedEvent += RoundStarted;
        RoundManager.Instance.RoundEndedEvent += RoundEnded;
    }

    void UnsubscribeAllEvents()
    {
        RoundManager.Instance.RoundStartedEvent -= RoundStarted;
        RoundManager.Instance.RoundEndedEvent -= RoundEnded;
    }

    void RoundStarted()
    {
        gladiatorManager.SpawnGladiators();
        enemyManager.SpawnEnemies();
    }

    void RoundEnded()
    {
        gladiatorManager.Cleanup();
        enemyManager.Cleanup();
    }
}
