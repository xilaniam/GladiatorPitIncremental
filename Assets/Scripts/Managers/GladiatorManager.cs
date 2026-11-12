using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GladiatorManager : GameEventListener
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

    List<Gladiator> spawnedGladiators = new List<Gladiator>();
    List<Vector3> freeSpawnSlots = new List<Vector3>();

    #region-------Initialization And Subscription -------
    void Awake()
    {
        ResetSpawnSlots();
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
        EventManager.OnCircleTickEvent += Attack;
        EventManager.RoundStartedEvent += SpawnGladiators;
        EventManager.RoundEndedEvent += Cleanup;
    }

    protected override void UnsubscribeEvents()
    {
        EventManager.OnCircleTickEvent -= Attack;
        EventManager.RoundStartedEvent -= SpawnGladiators;
        EventManager.RoundEndedEvent -= Cleanup;
    }
    #endregion

    public void SpawnGladiators()
    {
        int count = Mathf.Min(gladiatorCount.Value, maxGladiators);
        for (int i = 0; i < count; i++)
        {
            SpawnGladiator();
        }
    }

    public void Cleanup()
    {
        StopAllCoroutines();
        foreach (Gladiator gladiator in spawnedGladiators)
        {
            if (gladiator != null)
            {
                Destroy(gladiator.gameObject);
            }
        }
        spawnedGladiators.Clear();
        ResetSpawnSlots();
    }

    private void SpawnGladiator()
    {
        Vector3 spawnPos = TakeRandomSpawnSlot();
        Vector3 worldSpawnPos = transform.position + spawnPos;

        Vector3 directionToCenter = transform.position - worldSpawnPos;
        Quaternion rotation = Quaternion.LookRotation(directionToCenter);

        GameObject gladiatorObject = Instantiate(
            gladiatorPrefab,
            worldSpawnPos,
            rotation,
            transform
        );

        Gladiator gladiator = gladiatorObject.GetComponent<Gladiator>();
        gladiator.SetupWeapon(weaponPrefab.gameObject);

        spawnedGladiators.Add(gladiator);
    }

    private void Attack(List<Transform> targets)
    {
        if (targets.Count == 0) return;
        StartCoroutine(ExecuteAttackCoroutine(new List<Transform>(targets)));
    }

    IEnumerator ExecuteAttackCoroutine(List<Transform> targets)
    {
        int targetIndex = 0;

        for (int i = 0; i < spawnedGladiators.Count; i++)
        {
            targets.RemoveAll(t => t == null);
            if (targets.Count == 0) yield break;

            Transform target = targets[targetIndex++ % targets.Count];
            spawnedGladiators[i].Attack(weaponThrowCount.Value, target);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.35f));
        }
    }

    void ResetSpawnSlots()
    {
        freeSpawnSlots.Clear();
        for (int i = 0; i < maxGladiators; i++)
        {
            float angle = i * Mathf.PI * 2 / maxGladiators;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ringOuterRadius;
            freeSpawnSlots.Add(pos);
        }
    }

    Vector3 TakeRandomSpawnSlot()
    {
        int index = Random.Range(0, freeSpawnSlots.Count);
        Vector3 pos = freeSpawnSlots[index];
        freeSpawnSlots.RemoveAt(index);
        return pos;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ringOuterRadius);
    }
}
