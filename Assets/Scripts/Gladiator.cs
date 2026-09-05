using System.Collections;
using UnityEngine;

public class Gladiator : MonoBehaviour
{
    [Header("Throw Settings")]
    [SerializeField] private Transform throwPoint;       // where weapons spawn from (e.g. hand)
    [SerializeField] private float throwSpeed = 15f;
    [SerializeField] private float delayBetweenThrows = 0.2f;

    private GameObject currentWeaponPrefab;
    private Coroutine attackRoutine;
    public void SetupWeapon(GameObject weaponPrefab)
    {
        currentWeaponPrefab = weaponPrefab;
    }
    public void Attack(int weaponThrowCount, Vector3 targetPosition)
    {
        if (currentWeaponPrefab == null)
        {
            Debug.LogWarning($"{name}: No weapon assigned. Call SetupWeapon() first.");
            return;
        }

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackRoutine = StartCoroutine(ThrowNWeapons(weaponThrowCount, targetPosition));
    }

    private IEnumerator ThrowNWeapons(int weaponThrowCount, Vector3 targetPosition)
    {
        for (int i = 0; i < weaponThrowCount; i++)
        {
            ThrowSingle(targetPosition);

            if (i < weaponThrowCount - 1)
            {
                yield return new WaitForSeconds(delayBetweenThrows);
            }
        }

        attackRoutine = null;
    }

    private void ThrowSingle(Vector3 targetPosition)
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