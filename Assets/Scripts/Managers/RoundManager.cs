using System;
using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [SerializeField] private FloatVariable currentRoundTimer; //15
    [SerializeField] private RoundUI RoundUI;

    public event Action RoundStartedEvent;
    public event Action RoundEndedEvent;

    public bool IsRoundActive { get; private set; }

    private Coroutine roundCoroutine;
    private float currentTime = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        IsRoundActive = false;
        Instance = this;
    }

    IEnumerator Start()
    {
        yield return null;
        StartRound();
    }

    private void Update()
    {
        if (!IsRoundActive) return;
        //Start Round Timer
        currentTime += Time.deltaTime;
        RoundUI.UpdateRoundTimerUI(currentTime, currentRoundTimer.Value);
        if (currentTime >= currentRoundTimer.Value)
        {
            EndRound();
        }
    }

    public void StartRound()
    {
        if (IsRoundActive)
            return;

        IsRoundActive = true;

        RoundStartedEvent?.Invoke();

        //roundCoroutine = StartCoroutine(RoundTimerCoroutine());
    }

    public void EndRound()
    {
        if (!IsRoundActive)
            return;

       /* if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }*/

        IsRoundActive = false;
        currentTime = 0;
        RoundEndedEvent?.Invoke();
    }

    private IEnumerator RoundTimerCoroutine()
    {
        yield return new WaitForSeconds(currentRoundTimer.Value);
        EndRound();
    }
}