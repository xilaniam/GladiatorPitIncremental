using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour,IDamageable
{
    [SerializeField] private float health = 7f;
    [SerializeField] private IntVariable coinValue;
    [Header("Roaming Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 4f;
    [SerializeField] private float arrivalThreshold = 0.2f;
    [Header("Materials")]
    [SerializeField] List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

    private float maxRoamRadius;
    private float minRoamRadius;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float waitTimer;
    private bool isWaiting;
    private bool isDead;

    public System.Action<Enemy> OnEnemyDeadAction;
    public System.Action<Enemy> OnDamageTakenAction;


    public float Health => health;
    public bool IsDead => isDead;
    public int CoinValue => coinValue.Value;

    private void Awake()
    {
        startPosition = transform.position;
    }

    public void Setup(float ringRadius)
    {
        maxRoamRadius = ringRadius;
        minRoamRadius = ringRadius * 0.2f; 
    }

    private void Start()
    {
        PickNewTarget();
    }

    private void Update()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewTarget();
            }
            return;
        }

        MoveTowardsTarget();

        // Check if arrived
        if (Vector3.Distance(transform.position, targetPosition) <= arrivalThreshold)
        {
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void PickNewTarget()
    {
        float randomRadius = Random.Range(minRoamRadius, maxRoamRadius);
        Vector2 randomCircle = Random.insideUnitCircle * randomRadius;
        targetPosition = startPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
        targetPosition.y = transform.position.y; // keep same height, adjust if needed
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        OnDamageTakenAction?.Invoke(this);
        FlashEffect();
        if (health <= 0) 
        {
            Die();
            return; 
        }
    }

    void FlashEffect()
    {
        StartCoroutine(FlashEffectCoroutine());
    }

    void Die()
    {
        isDead = true;
        OnEnemyDeadAction?.Invoke(this);
        Destroy(gameObject);
    }
    IEnumerator FlashEffectCoroutine()
    {
        float flashDuration = 0.1f;
        // 0 -> 1
        for (float t = 0; t < flashDuration; t += Time.deltaTime)
        {
            float value = t / flashDuration;

            SetFlashValue(value);

            yield return null;
        }

        // 1 -> 0
        for (float t = 0; t < flashDuration; t += Time.deltaTime)
        {
            float value = 1f - (t / flashDuration);

            SetFlashValue(value);

            yield return null;
        }

        SetFlashValue(0);
    }
    void SetFlashValue(float value)
    {
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.material.SetFloat("_FlashAmount", value);
        }
    }
}