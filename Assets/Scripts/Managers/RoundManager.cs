using System;
using System.Collections;
using UnityEngine;

public class RoundManager : GameEventListener
{
    public static RoundManager Instance { get; private set; }

    [SerializeField] private FloatVariable currentRoundTimer; //15
    [SerializeField] private RoundUI RoundUI;

    public bool IsRoundActive { get; private set; }

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
        SubscribeEvents();
        StartRound();
    }

    protected override void SubscribeEvents()
    {
        MenuUI.OnContinueButtonClickedEvent += StartRound;
        SkillManager.OnContinueButtonClickedEvent += StartRound;
    }

    protected override void UnsubscribeEvents()
    {
        MenuUI.OnContinueButtonClickedEvent -= StartRound;
        SkillManager.OnContinueButtonClickedEvent -= StartRound;
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

    private void StartRound()
    {
        if (IsRoundActive)
            return;

        IsRoundActive = true;
        EventManager.RoundStartedEvent?.Invoke();
    }

    private void EndRound()
    {
        if (!IsRoundActive)
            return;

        IsRoundActive = false;
        currentTime = 0;
        EventManager.RoundEndedEvent?.Invoke();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}