using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] List<Enemy> enemyPrefabs = new List<Enemy>();
    [SerializeField] float ringRadius;
    [SerializeField] int initialSpawnCount;

    private List<Enemy> spawnedEnemy = new List<Enemy>();

    private int currentEnemyCount;
    void Awake()
    {
        currentEnemyCount = initialSpawnCount;
    }
    public void SpawnEnemies()
    {
        for(int i = 0; i< currentEnemyCount; i++)
        {
            Vector3 spawnPosition = GetRandomPositionInsideCircle();
            Enemy randomEnemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            Enemy enemy = Instantiate(randomEnemyPrefab, spawnPosition, Quaternion.identity,transform);
            enemy.Setup(ringRadius,ClearDeadEnemy);
            spawnedEnemy.Add(enemy);
        }
    }

    public void Cleanup()
    {
        foreach (Enemy enemy in spawnedEnemy)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        spawnedEnemy.Clear();
    }
    Vector3 GetRandomPositionInsideCircle()
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        float radius = Random.Range(0f, ringRadius);
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(x, 0f, z);
    }
    void ClearDeadEnemy(Enemy deadEnemy)
    {
        EventManager.InvokeCoinDroppedEvent(deadEnemy.CoinValue);
        spawnedEnemy.RemoveAll(enemy => enemy.IsDead);
        if(spawnedEnemy.Count == 0)
        {
            RoundManager.Instance.EndRound();
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, ringRadius);
    }
}
