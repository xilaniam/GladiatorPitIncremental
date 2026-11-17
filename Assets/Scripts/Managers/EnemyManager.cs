using System.Collections.Generic;
using System.Linq;
using SMPro.SkillTree;
using UnityEngine;

public class EnemyManager : GameEventListener
{
    [SerializeField] List<Enemy> enemyPrefabs = new List<Enemy>();
    [SerializeField] List<AnimalType> startingEnemies = new List<AnimalType> { AnimalType.Bunny };
    [SerializeField] float ringRadius;
    [Header("Spawn Weights")]
    [SerializeField, Tooltip("Spawn weight by how far an animal is below the newest unlocked one (first = newest). Animals further down use the last value.")]
    float[] tierWeights = { 50f, 25f, 12f, 6f, 4f, 3f };
    [Header("Stats")]
    [SerializeField] StatRef enemyCount;
    [SerializeField] StatRef alphaChance;
    [SerializeField] StatRef goldenChance;

    private List<Enemy> spawnedEnemy = new List<Enemy>();
    private HashSet<AnimalType> unlockedTypes = new HashSet<AnimalType>();
    private List<Enemy> availablePrefabs = new List<Enemy>();

    public IReadOnlyList<Enemy> AliveEnemies => spawnedEnemy;
    public int UnlockedTiers => unlockedTypes.Count;

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

    public Enemy GetPrefab(AnimalType type) => enemyPrefabs.FirstOrDefault(p => p != null && p.AnimalType == type);

    public void UnlockEnemy(AnimalType type)
    {
        if (unlockedTypes.Add(type))
            RebuildAvailablePrefabs();
    }

    void RebuildAvailablePrefabs()
    {
        availablePrefabs = enemyPrefabs
            .Where(p => p != null && unlockedTypes.Contains(p.AnimalType))
            .OrderBy(p => p.AnimalType)
            .ToList();
    }

    #endregion

    #region Spawning

    public void SpawnEnemies()
    {
        int count = enemyCount.IntValue;
        int guaranteed = Mathf.Min(availablePrefabs.Count, count);
        for (int i = 0; i < guaranteed; i++)
            SpawnEnemy(availablePrefabs[availablePrefabs.Count - 1 - i]);

        for (int i = guaranteed; i < count; i++)
            SpawnEnemy();
    }

    public bool TrySpawnExtra()
    {
        if (spawnedEnemy.Count >= enemyCount.IntValue) return false;

        SpawnEnemy();
        return true;
    }

    public void SpawnEnemy() => SpawnEnemy(PickEnemyPrefab());

    void SpawnEnemy(Enemy enemyPrefab)
    {
        if (enemyPrefab == null) return;

        Vector3 spawnPosition = GetRandomPositionInsideCircle();
        Enemy enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
        enemy.Setup(transform.position, ringRadius);
        if (Random.value < goldenChance.Value) enemy.MakeGolden();
        spawnedEnemy.Add(enemy);

        enemy.OnEnemyDeadAction += ClearDeadEnemy;
        enemy.OnDamageTakenAction += OnDamageTaken;
    }

    Enemy PickEnemyPrefab()
    {
        if (availablePrefabs.Count == 0)
        {
            Debug.LogWarning("EnemyManager: no unlocked enemy prefabs. Check startingEnemies and enemyPrefabs.");
            return null;
        }

        int index = PickWeightedTier(availablePrefabs.Count);
        bool isNewestTier = index == availablePrefabs.Count - 1;
        if (!isNewestTier && Random.value < alphaChance.Value) index++;

        return availablePrefabs[index];
    }

    int PickWeightedTier(int tierCount)
    {
        int newest = tierCount - 1;
        float total = 0f;
        for (int tier = 0; tier < tierCount; tier++)
            total += TierWeight(newest - tier);

        float roll = Random.value * total;
        for (int tier = newest; tier > 0; tier--)
        {
            roll -= TierWeight(newest - tier);
            if (roll < 0f) return tier;
        }
        return 0;
    }

    float TierWeight(int stepsBelowNewest)
    {
        if (tierWeights == null || tierWeights.Length == 0) return 1f;
        return tierWeights[Mathf.Min(stepsBelowNewest, tierWeights.Length - 1)];
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
        deadEnemy.OnEnemyDeadAction -= ClearDeadEnemy;
        deadEnemy.OnDamageTakenAction -= OnDamageTaken;
        spawnedEnemy.RemoveAll(enemy => enemy == null || enemy.IsDead);

        EventManager.InvokeEnemyDeadEvent(deadEnemy);

        if (spawnedEnemy.Count == 0)
        {
            SpawnEnemy();
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
