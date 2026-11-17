using System.Collections;
using UnityEngine;

public class Gladiator : MonoBehaviour
{
    [Header("Throw Settings")]
    [SerializeField] private Transform throwPoint;       // where weapons spawn from (e.g. hand)
    [SerializeField] private float throwSpeed = 15f;

    [Header("Held Spear")]
    [SerializeField] private GameObject heldSpear;
    [SerializeField, Tooltip("Seconds after a throw before the held spear reappears")] private float regripDelay = 0.25f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private GameObject currentWeaponPrefab;
    private Transform currentTarget;
    private int remainingThrows;
    private Coroutine regripRoutine;

    public bool IsAttacking => remainingThrows > 0;

    private void OnDisable()
    {
        regripRoutine = null;
        if (heldSpear != null) heldSpear.SetActive(true);
    }

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
        HideHeldSpear();

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

    private void HideHeldSpear()
    {
        if (heldSpear == null) return;

        heldSpear.SetActive(false);
        if (regripRoutine != null) StopCoroutine(regripRoutine);
        regripRoutine = StartCoroutine(RegripAfterDelay());
    }

    private IEnumerator RegripAfterDelay()
    {
        yield return new WaitForSeconds(regripDelay);
        heldSpear.SetActive(true);
        regripRoutine = null;
    }
}
