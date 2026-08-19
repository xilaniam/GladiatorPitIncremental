using System;
using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [SerializeField] private float currentRoundTimer = 15f;

    public event Action RoundStartedEvent;
    public event Action RoundEndedEvent;

    public bool IsRoundActive { get; private set; }

    private Coroutine roundCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    IEnumerator Start()
    {
        yield return null;
        StartRound();
    }

    public void StartRound()
    {
        if (IsRoundActive)
            return;

        IsRoundActive = true;

        RoundStartedEvent?.Invoke();

        roundCoroutine = StartCoroutine(RoundTimerCoroutine());
    }

    public void EndRound()
    {
        if (!IsRoundActive)
            return;

        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }

        IsRoundActive = false;

        RoundEndedEvent?.Invoke();
    }

    private IEnumerator RoundTimerCoroutine()
    {
        yield return new WaitForSeconds(currentRoundTimer);
        EndRound();
    }
}