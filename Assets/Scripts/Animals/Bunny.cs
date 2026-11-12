using UnityEngine;

public class BunnyEnemy : Enemy
{
    [Header("Hop Settings")]
    [SerializeField] private float hopHeight = 0.75f;
    [SerializeField] private float hopDuration = 0.4f;
    [SerializeField] private float minHopDistance = 0.4f;
    [SerializeField] private float maxHopDistance = 1.0f;

    private bool isHopping;
    private float hopElapsed;
    private Vector3 hopStartPosition;
    private Vector3 hopEndPosition;
    private float groundY;

    protected override void Awake()
    {
        base.Awake();
        groundY = transform.position.y;
    }

    protected override float GetMoveDistance()
    {
        return Random.Range(minHopDistance, maxHopDistance);
    }

    protected override void OnKnockbackMoved(Vector3 delta)
    {
        hopStartPosition += delta;
        hopEndPosition = ClampToRoamArea(hopEndPosition + delta);
    }

    protected override void HandleMovement()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewTarget();
                BeginHop();
            }
            return;
        }

        if (isHopping)
        {
            UpdateHop();
            return;
        }

        BeginHop();
    }

    private void BeginHop()
    {
        hopStartPosition = transform.position;

        Vector3 toTarget = targetPosition - hopStartPosition;
        toTarget.y = 0f;

        hopEndPosition = hopStartPosition + toTarget;
        hopEndPosition.y = groundY;

        if (toTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized);
        }

        hopElapsed = 0f;
        isHopping = true;
    }

    private void UpdateHop()
    {
        hopElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(hopElapsed / hopDuration);

        Vector3 flatPosition = Vector3.Lerp(hopStartPosition, hopEndPosition, t);
        float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;
        transform.position = new Vector3(flatPosition.x, groundY + arc, flatPosition.z);

        if (t >= 1f)
        {
            isHopping = false;
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }
}