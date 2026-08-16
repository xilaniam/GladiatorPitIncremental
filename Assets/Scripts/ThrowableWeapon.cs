using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowableWeapon : MonoBehaviour
{
    [SerializeField] float damageAmount = 3f;

    [Header("Settings")]
    [SerializeField] float defaultSpeed = 15f;
    [SerializeField] bool rotateTowardsTarget = true;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] float arcHeight = 2f;

    private Rigidbody rb;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float speed;
    private float travelTime;
    private float duration;

    private bool isLaunched;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
    }

    public void Init(Vector3 target, float throwSpeed = -1f)
    {
        startPosition = transform.position;
        targetPosition = target;

        speed = throwSpeed > 0f
            ? throwSpeed
            : defaultSpeed;

        float distance = Vector3.Distance(
            startPosition,
            targetPosition
        );

        duration = distance / speed;

        travelTime = 0f;
        isLaunched = true;

        if (rotateTowardsTarget)
        {
            Vector3 direction = targetPosition - startPosition;

            if (direction != Vector3.zero)
            {
                transform.rotation =
                    Quaternion.LookRotation(direction);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isLaunched)
            return;

        travelTime += Time.fixedDeltaTime;

        float t = Mathf.Clamp01(travelTime / duration);

        Vector3 position = GetPosition(t);

        // Calculate next position for rotation
        float nextT = Mathf.Clamp01(
            (travelTime + Time.fixedDeltaTime) / duration
        );

        Vector3 nextPosition = GetPosition(nextT);

        if (rotateTowardsTarget)
        {
            Vector3 direction = nextPosition - position;

            if (direction != Vector3.zero)
            {
                rb.MoveRotation(
                    Quaternion.LookRotation(direction)
                );
            }
        }

        rb.MovePosition(position);

        if (t >= 1f)
        {
            rb.MovePosition(targetPosition);
            Destroy(gameObject);
        }
    }

    private Vector3 GetPosition(float t)
    {
        Vector3 position = Vector3.Lerp(
            startPosition,
            targetPosition,
            t
        );

        position.y +=
            Mathf.Sin(t * Mathf.PI) * arcHeight;

        return position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        if (((1 << hitObject.layer) & hitMask) == 0)
            return;

        if (hitObject.TryGetComponent<IDamageable>(
            out var damageable))
        {
            damageable.TakeDamage(damageAmount);
        }

        Destroy(gameObject);
    }
}