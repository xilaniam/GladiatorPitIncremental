using System.Collections;
using UnityEngine;

public class TargetCircle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform ringCenter;   // the brown ring object (defines center & bounds)
    [SerializeField] private Camera cam;               // main camera, auto-assigned if left empty

    [Header("Settings")]
    [SerializeField] private float radius = 1f;         // max distance handle can move from center
    [SerializeField] private float followSpeed = 15f;
    [SerializeField] private float attackSpeedPerSecond = 0.5f;// 0 = instant snap to mouse
    [SerializeField] private LayerMask groundLayer;      // optional: layer for raycast plane

    private Plane movementPlane;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        // Plane facing up (Y axis), positioned at the ring's height
        movementPlane = new Plane(Vector3.up, ringCenter.position);
    }

    private void Start()
    {
        StartCoroutine(InvokeAttackEvent());
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
            Vector3 clampedOffset = Vector3.ClampMagnitude(offset, radius);

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
    }

    IEnumerator InvokeAttackEvent()
    {
        while (true)
        {
            EventManager.InvokeCircleTick(transform.position);
            yield return new WaitForSeconds(attackSpeedPerSecond);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (ringCenter == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ringCenter.position, radius);
    }
}