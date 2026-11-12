using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : GameEventListener
{
    [SerializeField] List<Enemy> enemyPrefabs = new List<Enemy>();
    [SerializeField] List<AnimalType> startingEnemies = new List<AnimalType> { AnimalType.Bunny };
    [SerializeField] float ringRadius;
    [SerializeField] IntVariable enemyCount;

    private List<Enemy> spawnedEnemy = new List<Enemy>();
    private HashSet<AnimalType> unlockedTypes = new HashSet<AnimalType>();
    private List<Enemy> availablePrefabs = new List<Enemy>();

    private void Awake()
    {
        foreach (AnimalType type in startingEnemies)
            unlockedTypes.Add(type);

        RebuildAvailablePrefabs();
    }

    private void Start()
    {
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    protected override void SubscribeEvents()
    {
        EventManager.RoundStartedEvent += SpawnEnemies;
        EventManager.RoundEndedEvent += Cleanup;
    }

    protected override void UnsubscribeEvents()
    {
        EventManager.RoundStartedEvent -= SpawnEnemies;
        EventManager.RoundEndedEvent -= Cleanup;
    }

    #region Unlocking

    public bool IsUnlocked(AnimalType type) => unlockedTypes.Contains(type);

    public void UnlockEnemy(AnimalType type)
    {
        if (unlockedTypes.Add(type))
            RebuildAvailablePrefabs();
    }

    void RebuildAvailablePrefabs()
    {
        availablePrefabs = enemyPrefabs
            .Where(p => p != null && unlockedTypes.Contains(p.AnimalType))
            .ToList();
    }

    #endregion

    #region Spawning

    public void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount.Value; i++)
        {
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        Enemy randomEnemyPrefab = GetRandomEnemy();
        if (randomEnemyPrefab == null) return;

        Vector3 spawnPosition = GetRandomPositionInsideCircle();
        Enemy enemy = Instantiate(randomEnemyPrefab, spawnPosition, Quaternion.identity, transform);
        enemy.Setup(transform.position, ringRadius);
        spawnedEnemy.Add(enemy);

        enemy.OnEnemyDeadAction += ClearDeadEnemy;
        enemy.OnDamageTakenAction += OnDamageTaken;
    }

    Enemy GetRandomEnemy()
    {
        if (availablePrefabs.Count == 0)
        {
            Debug.LogWarning("EnemyManager: no unlocked enemy prefabs. Check startingEnemies and enemyPrefabs.");
            return null;
        }

        return availablePrefabs[Random.Range(0, availablePrefabs.Count)];
    }

    public void Cleanup()
    {
        foreach (Enemy enemy in spawnedEnemy)
        {
            if (enemy != null)
            {
                enemy.OnEnemyDeadAction -= ClearDeadEnemy;
                enemy.OnDamageTakenAction -= OnDamageTaken;
                Destroy(enemy.gameObject);
            }
        }
        spawnedEnemy.Clear();
    }

    void ClearDeadEnemy(Enemy deadEnemy)
    {
        EventManager.InvokeCoinDroppedEvent(deadEnemy.CoinValue);
        EventManager.InvokeEnemyDeadEvent(deadEnemy);

        deadEnemy.OnEnemyDeadAction -= ClearDeadEnemy;
        deadEnemy.OnDamageTakenAction -= OnDamageTaken;

        spawnedEnemy.RemoveAll(enemy => enemy == null || enemy.IsDead);
        if (spawnedEnemy.Count == 0)
        {
            SpawnEnemy();
            //EventManager.InvokeAllEnemyClearedEvent();
        }
    }

    void OnDamageTaken(Enemy damageable)
    {
        EventManager.InvokeEnemyDamagedEvent(damageable);
    }

    Vector3 GetRandomPositionInsideCircle()
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        float radius = Random.Range(0f, ringRadius);
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(transform.position.x + x, 0f, transform.position.z + z);
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, ringRadius);
    }
}