using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] List<Enemy> enemyPrefabs = new List<Enemy>();
    [SerializeField] float ringRadius;
    [SerializeField] int initialSpawnCount;
    void Start()
    {
        SpawnEnemy();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnEnemy()
    {
        for(int i = 0; i< initialSpawnCount; i++)
        {
            Vector3 spawnPosition = GetRandomPositionInsideCircle();
            Enemy randomEnemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            Enemy enemy = Instantiate(randomEnemyPrefab, spawnPosition, Quaternion.identity,transform);
            enemy.Setup(ringRadius);
        }
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
