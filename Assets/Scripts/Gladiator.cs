using UnityEngine;

public class Gladiator : MonoBehaviour
{
    [Header("Throw Settings")]
    [SerializeField] private Transform throwPoint;       // where weapons spawn from (e.g. hand)
    [SerializeField] private float throwSpeed = 15f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private GameObject currentWeaponPrefab;
    private Transform currentTarget;
    private int remainingThrows;

    public bool IsAttacking => remainingThrows > 0;

    public void SetupWeapon(GameObject weaponPrefab)
    {
        currentWeaponPrefab = weaponPrefab;
    }

    public void Attack(int weaponThrowCount, Transform target)
    {
        if (currentWeaponPrefab == null)
        {
            Debug.LogWarning($"{name}: No weapon assigned. Call SetupWeapon() first.");
            return;
        }

        currentTarget = target;
        if (IsAttacking) return;

        remainingThrows = weaponThrowCount;
        animator.SetTrigger("Attack");
    }

    public void SpawnWeapon()
    {
        if (remainingThrows <= 0) return;

        if (currentTarget == null)
        {
            remainingThrows = 0;
            return;
        }

        remainingThrows--;
        ThrowAt(currentTarget.position);

        if (remainingThrows > 0)
            animator.SetTrigger("Attack");
    }

    private void ThrowAt(Vector3 targetPosition)
    {
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position;

        GameObject weaponInstance = Instantiate(currentWeaponPrefab, spawnPos, Quaternion.identity);

        ThrowableWeapon throwable = weaponInstance.GetComponent<ThrowableWeapon>();
        if (throwable != null)
        {
            throwable.Init(targetPosition, throwSpeed);
        }
        else
        {
            Debug.LogWarning($"{currentWeaponPrefab.name} has no ThrowableWeapon component.");
        }
    }
}
