using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GladiatorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject gladiatorPrefab;
    [SerializeField] ThrowableWeapon weaponPrefab;
    [Header("DataSetup")]
    [SerializeField] float ringOuterRadius = 5f;
    [SerializeField] int maxGladiators = 10;
    [Header("Variables")]
    [SerializeField] IntVariable gladiatorCount;
    [SerializeField] IntVariable weaponThrowCount;

    List<GameObject> spawnedGladiators = new List<GameObject>();
    HashSet<Vector3> spawnPositions = new HashSet<Vector3>();

    void Awake()
    {
        GenerateSpawnPos();
    }

    private void Start()
    {
        EventManager.OnCircleTickEvent += Attack;
    }

    private void OnDestroy()
    {
        EventManager.OnCircleTickEvent -= Attack;
    }

    public void SpawnGladiators()
    {
        if (gladiatorCount.Value > maxGladiators) return;
        for (int i = 0; i < gladiatorCount.Value; i++)
        {
            SpawnGladiator();
        }
    }

    public void Cleanup()
    {
        foreach (GameObject gladiator in spawnedGladiators)
        {
            if (gladiator != null)
            {
                Destroy(gladiator);
            }
        }
        spawnedGladiators.Clear();
    }

    private void SpawnGladiator()
    {
        Vector3 spawnPos = GetRandomUniquePositions();
        Vector3 worldSpawnPos = transform.position + spawnPos;

        Vector3 directionToCenter = transform.position - worldSpawnPos;
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

    private void Attack(List<Transform> targets)
    {
       StartCoroutine(ExecuteAttackCoroutine(targets));
    }

    IEnumerator ExecuteAttackCoroutine(List<Transform> targets)
    {
        if(targets.Count == 0)
        {
            yield break;
        }

        int currentTarget = 0;
            
        foreach (GameObject gladiator in spawnedGladiators)
        {
            float randomDuration = Random.Range(0.1f, 0.35f);
            Gladiator gladiatorScript = gladiator.GetComponent<Gladiator>();
            if (gladiatorScript != null)
            {
                gladiatorScript.Attack(weaponThrowCount.Value, targets[currentTarget].position);
                yield return new WaitForSeconds(randomDuration);
                if (targets.Count <= 0) yield break;
                currentTarget = (currentTarget + 1) % targets.Count;
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
