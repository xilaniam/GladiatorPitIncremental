using System;
using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [SerializeField] float speed = 30f;
    [SerializeField] float arcHeight = 1.5f;
    [SerializeField] float aimHeight = 1f;

    private Enemy target;
    private Vector3 start;
    private Vector3 lastAimPoint;
    private float duration;
    private float elapsed;
    private Action<Vector3, Enemy> onArrive;

    public void Launch(Vector3 from, Enemy target, Action<Vector3, Enemy> onArrive)
    {
        this.target = target;
        this.onArrive = onArrive;
        start = from;
        transform.position = from;
        lastAimPoint = AimPoint();
        duration = Mathf.Max(0.05f, Vector3.Distance(from, lastAimPoint) / speed);
        elapsed = 0f;
    }

    private void Update()
    {
        if (target != null && !target.IsDead) lastAimPoint = AimPoint();

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        Vector3 next = Vector3.Lerp(start, lastAimPoint, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);

        Vector3 direction = next - transform.position;
        if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
        transform.position = next;

        if (t >= 1f) Arrive();
    }

    private void Arrive()
    {
        Enemy hit = target != null && !target.IsDead ? target : null;
        onArrive?.Invoke(transform.position, hit);
        Destroy(gameObject);
    }

    private Vector3 AimPoint() => target.transform.position + Vector3.up * aimHeight;
}
