using UnityEngine;

public interface IDamageable
{
    public float Health { get; }
    public void TakeDamage(float damage);
    public void TakeDamage(float damage, Vector3 hitDirection);
}
