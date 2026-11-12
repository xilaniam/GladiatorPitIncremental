using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] protected float health = 7f;
    [SerializeField] protected IntVariable coinValue;
    [SerializeField] protected AnimalType animalType;
    [Header("Roaming Settings")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float rotationSpeed = 5f;
    [SerializeField] protected float minWaitTime = 1f;
    [SerializeField] protected float maxWaitTime = 4f;
    [SerializeField] protected float arrivalThreshold = 0.2f;

    [Header("Move Distance")]
    [SerializeField] protected float minMoveDistance = 2f;
    [SerializeField] protected float maxMoveDistance = 5f;
    [SerializeField] protected int maxDirectionAttempts = 8;

    [Header("Knockback")]
    [SerializeField] protected float minKnockbackForce = 6f;
    [SerializeField] protected float maxKnockbackForce = 10f;
    [SerializeField] protected float recoveryStiffness = 80f;
    [SerializeField] protected float recoveryDamping = 10f;
    [SerializeField] protected float maxKnockbackOffset = 1f;

    [Header("Wobble")]
    [SerializeField] protected float minWobbleKick = 250f;
    [SerializeField] protected float maxWobbleKick = 450f;
    [SerializeField] protected float wobbleStiffness = 200f;
    [SerializeField] protected float wobbleDamping = 8f;
    [SerializeField] protected float maxWobbleAngle = 30f;

    [Header("Materials")]
    [SerializeField] protected List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

    protected float roamRadius;
    protected Vector3 roamCenter;
    protected Vector3 targetPosition;
    protected Vector3 knockbackVelocity;
    protected Vector3 knockbackOffset;
    protected float wobbleAngle;
    protected float wobbleVelocity;
    protected float waitTimer;
    protected bool isWaiting;
    protected bool isDead;

    private float appliedWobbleAngle;

    public System.Action<Enemy> OnEnemyDeadAction;
    public System.Action<Enemy> OnDamageTakenAction;

    public float Health => health;
    public bool IsDead => isDead;
    public int CoinValue => coinValue.Value;
    public AnimalType AnimalType => animalType;

    protected virtual void Awake()
    {
        roamCenter = transform.position;
    }

    public virtual void Setup(Vector3 center, float radius)
    {
        roamCenter = center;
        roamRadius = radius;
    }

    protected virtual void Start()
    {
        PickNewTarget();
    }

    protected virtual void Update()
    {
        RemoveWobble();
        HandleMovement();
        UpdateKnockback();
        UpdateWobble();
        ApplyWobble();
    }

    protected virtual void HandleMovement()
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

        if (Vector3.Distance(transform.position, targetPosition) <= arrivalThreshold)
        {
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    protected virtual void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    protected virtual float GetMoveDistance()
    {
        return Random.Range(minMoveDistance, maxMoveDistance);
    }

    protected virtual Vector3 GetMoveDirection()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    protected bool IsInsideRoamArea(Vector3 position)
    {
        Vector3 offset = position - roamCenter;
        offset.y = 0f;
        return offset.sqrMagnitude <= roamRadius * roamRadius;
    }

    protected Vector3 ClampToRoamArea(Vector3 position)
    {
        if (roamRadius <= 0f)
        {
            return position;
        }

        Vector3 offset = position - roamCenter;
        offset.y = 0f;

        if (offset.sqrMagnitude <= roamRadius * roamRadius)
        {
            return position;
        }

        Vector3 clamped = roamCenter + offset.normalized * roamRadius;
        clamped.y = position.y;
        return clamped;
    }

    protected virtual void PickNewTarget()
    {
        Vector3 origin = transform.position;
        float distance = GetMoveDistance();

        for (int i = 0; i < maxDirectionAttempts; i++)
        {
            Vector3 candidate = origin + GetMoveDirection() * distance;
            candidate.y = origin.y;

            if (IsInsideRoamArea(candidate))
            {
                targetPosition = candidate;
                return;
            }
        }

        Vector3 toCenter = roamCenter - origin;
        toCenter.y = 0f;

        float step = toCenter.magnitude > 0.0001f
            ? Mathf.Min(distance, toCenter.magnitude)
            : Mathf.Min(distance, roamRadius);

        Vector3 fallbackDir = toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized : GetMoveDirection();

        targetPosition = origin + fallbackDir * step;
        targetPosition.y = origin.y;
    }

    public virtual void TakeDamage(float damage, Vector3 hitDirection)
    {
        if (isDead) return;
        ApplyKnockback(hitDirection);
        TakeDamage(damage);
    }

    public virtual void ApplyKnockback(Vector3 hitDirection)
    {
        hitDirection.y = 0f;

        if (hitDirection.sqrMagnitude < 0.0001f)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            hitDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        Vector3 direction = hitDirection.normalized;

        knockbackVelocity = direction * Random.Range(minKnockbackForce, maxKnockbackForce);

        float side = Vector3.Dot(direction, transform.right);
        float sign = Mathf.Approximately(side, 0f)
            ? (Random.value < 0.5f ? -1f : 1f)
            : -Mathf.Sign(side);

        wobbleVelocity = sign * Random.Range(minWobbleKick, maxWobbleKick);
    }

    protected virtual void OnKnockbackMoved(Vector3 delta)
    {
    }

    private void UpdateKnockback()
    {
        if (knockbackOffset == Vector3.zero && knockbackVelocity == Vector3.zero)
        {
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, 0.05f);

        Vector3 acceleration = -recoveryStiffness * knockbackOffset - recoveryDamping * knockbackVelocity;
        knockbackVelocity += acceleration * dt;

        Vector3 newOffset = knockbackOffset + knockbackVelocity * dt;

        if (newOffset.sqrMagnitude > maxKnockbackOffset * maxKnockbackOffset)
        {
            newOffset = newOffset.normalized * maxKnockbackOffset;
        }

        bool settled = newOffset.sqrMagnitude < 0.0001f && knockbackVelocity.sqrMagnitude < 0.0025f;

        Vector3 delta = settled ? -knockbackOffset : newOffset - knockbackOffset;
        Vector3 applied = ShiftByKnockback(delta);

        if (settled)
        {
            knockbackOffset = Vector3.zero;
            knockbackVelocity = Vector3.zero;
        }
        else
        {
            knockbackOffset += applied;
        }
    }

    private Vector3 ShiftByKnockback(Vector3 delta)
    {
        Vector3 current = transform.position;
        Vector3 clamped = ClampToRoamArea(current + delta);
        Vector3 applied = clamped - current;

        transform.position = clamped;
        targetPosition = ClampToRoamArea(targetPosition + applied);
        OnKnockbackMoved(applied);

        return applied;
    }

    private void UpdateWobble()
    {
        if (wobbleAngle == 0f && wobbleVelocity == 0f)
        {
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, 0.05f);

        float acceleration = -wobbleStiffness * wobbleAngle - wobbleDamping * wobbleVelocity;
        wobbleVelocity += acceleration * dt;
        wobbleAngle = Mathf.Clamp(wobbleAngle + wobbleVelocity * dt, -maxWobbleAngle, maxWobbleAngle);

        if (Mathf.Abs(wobbleAngle) < 0.05f && Mathf.Abs(wobbleVelocity) < 1f)
        {
            wobbleAngle = 0f;
            wobbleVelocity = 0f;
        }
    }

    private void RemoveWobble()
    {
        if (appliedWobbleAngle == 0f)
        {
            return;
        }

        transform.rotation *= Quaternion.Euler(0f, 0f, -appliedWobbleAngle);
        appliedWobbleAngle = 0f;
    }

    private void ApplyWobble()
    {
        if (wobbleAngle == 0f)
        {
            return;
        }

        transform.rotation *= Quaternion.Euler(0f, 0f, wobbleAngle);
        appliedWobbleAngle = wobbleAngle;
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        health -= damage;
        OnDamageTakenAction?.Invoke(this);
        FlashEffect();
        if (health <= 0)
        {
            Die();
            return;
        }
    }

    protected void FlashEffect()
    {
        StartCoroutine(FlashEffectCoroutine());
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        OnEnemyDeadAction?.Invoke(this);
        Destroy(gameObject);
    }

    private IEnumerator FlashEffectCoroutine()
    {
        float flashDuration = 0.1f;
        for (float t = 0; t < flashDuration; t += Time.deltaTime)
        {
            SetFlashValue(t / flashDuration);
            yield return null;
        }

        for (float t = 0; t < flashDuration; t += Time.deltaTime)
        {
            SetFlashValue(1f - (t / flashDuration));
            yield return null;
        }

        SetFlashValue(0);
    }

    private void SetFlashValue(float value)
    {
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.material.SetFloat("_FlashAmount", value);
        }
    }
}

public enum AnimalType
{
    Bunny,
    Fox,
    Boar,
    Deer
}