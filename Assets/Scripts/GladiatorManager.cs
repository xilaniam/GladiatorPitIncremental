using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GladiatorManager : MonoBehaviour
{
    [SerializeField] GameObject gladiatorPrefab;
    [SerializeField] ThrowableWeapon weaponPrefab;
    [SerializeField] float ringOuterRadius = 5f;
    [SerializeField] int maxGladiators = 10;
    [SerializeField] int initialGladiators = 3;
    List<GameObject> spawnedGladiators = new List<GameObject>();
    HashSet<Vector3> spawnPositions = new HashSet<Vector3>();
    void Awake()
    {
        GenerateSpawnPos();
    }

    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            SpawnGladiator();
        }
        EventManager.OnCircleTickAction += Attack;
    }

    private void OnDestroy()
    {
        EventManager.OnCircleTickAction -= Attack;
    }

    private void SpawnGladiator()
    {
        Vector3 spawnPos = GetRandomUniquePositions();
        Vector3 worldSpawnPos = transform.position + spawnPos;

        // Direction from spawn position toward the center
        Vector3 directionToCenter = transform.position - worldSpawnPos;

        // Rotation so the gladiator faces the center
        Quaternion rotation = Quaternion.LookRotation(directionToCenter);

        GameObject gladiator = Instantiate(
            gladiatorPrefab,
            worldSpawnPos,
            rotation,
            transform
        );

        gladiator.GetComponent<Gladiator>().SetupWeapon(weaponPrefab.gameObject);

        spawnedGladiators.Add(gladiator);
    }

    private void Attack(Vector3 targetPosition)
    {
       StartCoroutine(ExecuteAttackCoroutine(targetPosition));
    }

    IEnumerator ExecuteAttackCoroutine(Vector3 targetPosition)
    {
        foreach (GameObject gladiator in spawnedGladiators)
        {
            Gladiator gladiatorScript = gladiator.GetComponent<Gladiator>();
            if (gladiatorScript != null)
            {
                gladiatorScript.Attack(1, targetPosition);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void GenerateSpawnPos()
    {
        for(int i = 0; i<maxGladiators; i++)
        {
            float angle = i * Mathf.PI * 2 / maxGladiators;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ringOuterRadius;
            spawnPositions.Add(pos);
        }
    }

    Vector3 GetRandomUniquePositions()
    {
        if (spawnPositions.Count == 0)
        {
            GenerateSpawnPos();
        }
        int index = Random.Range(0, spawnPositions.Count);
        Vector3 pos = new List<Vector3>(spawnPositions)[index];
        spawnPositions.Remove(pos);
        return pos;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ringOuterRadius);
    }
}
