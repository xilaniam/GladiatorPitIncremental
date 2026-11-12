using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TargetCircle : GameEventListener
{
    [Header("References")]
    [SerializeField] private Transform ringCenter;
    [SerializeField] private Camera cam;

    [Header("Settings")]
    [SerializeField] private FloatVariable circleRadius;
    [SerializeField] private float boundaryRadius = 1f; 
    [SerializeField] private float followSpeed = 15f;  
    [SerializeField] private FloatVariable attackSpeedPerSecond; //0.5f
    [SerializeField] private float minAttackInterval = 0.05f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Image image;

    private Plane movementPlane;
    private List<Transform> overlappedTargets = new();
    private float attackTimer = 0f;
    private bool isActive;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        // Plane facing up (Y axis), positioned at the ring's height
        movementPlane = new Plane(Vector3.up, ringCenter.position);
    }

    private void Start()
    {
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (movementPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);

            // Clamp to radius around ring center (on the XZ plane)
            Vector3 offset = hitPoint - ringCenter.position;
            offset.y = 0f; // ignore vertical difference
            Vector3 clampedOffset = Vector3.ClampMagnitude(offset, boundaryRadius);

            Vector3 targetPosition = ringCenter.position + clampedOffset;
            targetPosition.y = transform.position.y; // keep handle's own height

            if (followSpeed <= 0f)
            {
                transform.position = targetPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            }
        }
        overlappedTargets.RemoveAll(target => target == null);

        TriggerAttack();
    }

    void TriggerAttack()
    {
        if (!isActive) return;

        float interval = Mathf.Max(minAttackInterval, attackSpeedPerSecond.Value);
        attackTimer += Time.deltaTime;
        if (attackTimer >= interval)
        {
            EventManager.InvokeCircleTick(overlappedTargets);
            attackTimer = 0f;
        }

        image.fillAmount = attackTimer / interval;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0)
            return;

        Transform target = other.transform;

        // Prevent duplicates
        if (!overlappedTargets.Contains(target))
        {
            overlappedTargets.Add(target);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) == 0)
            return;

        Transform target = other.transform;

        overlappedTargets.Remove(target);
    }

    private void OnDrawGizmosSelected()
    {
        if (ringCenter == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ringCenter.position, boundaryRadius);
    }

    void SetActive()
    {
        transform.localScale = Vector3.one * circleRadius.Value;
        isActive = true;
    }

    void SetDeactive()
    {
        isActive = false;
        attackTimer = 0f;
    }

    protected override void SubscribeEvents()
    {
        EventManager.RoundStartedEvent += SetActive;
        EventManager.RoundEndedEvent += SetDeactive;
    }

    protected override void UnsubscribeEvents()
    {
        EventManager.RoundStartedEvent -= SetActive;
        EventManager.RoundEndedEvent -= SetDeactive;
    }
}