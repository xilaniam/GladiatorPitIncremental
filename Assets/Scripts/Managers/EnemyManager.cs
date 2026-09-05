using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] List<Enemy> enemyPrefabs = new List<Enemy>();
    [SerializeField] float ringRadius;
    [SerializeField] IntVariable enemyCount;

    private List<Enemy> spawnedEnemy = new List<Enemy>();

    public void SpawnEnemies()
    {
        for(int i = 0; i< enemyCount.Value ; i++)
        {
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        Vector3 spawnPosition = GetRandomPositionInsideCircle();
        Enemy randomEnemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Enemy enemy = Instantiate(randomEnemyPrefab, spawnPosition, Quaternion.identity, transform);
        enemy.Setup(ringRadius);
        spawnedEnemy.Add(enemy);

        enemy.OnEnemyDeadAction += ClearDeadEnemy;
        enemy.OnDamageTakenAction += OnDamageTaken;
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

        spawnedEnemy.RemoveAll(enemy => enemy.IsDead);
        if(spawnedEnemy.Count == 0)
        {
            RoundManager.Instance.EndRound();
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
        return new Vector3(x, 0f, z);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, ringRadius);
    }
}
