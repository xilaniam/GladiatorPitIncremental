using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : BaseUIPopup
{
    [SerializeField] Button ContinueButton;
    [SerializeField] RoundSummaryUI roundSummary;

    public static event Action OnContinueButtonClickedEvent;
    private void Start()
    {
        ContinueButton.onClick.AddListener(OnContinueButtonClicked);

        EventManager.RoundResultsReadyEvent += OpenPopup;
    }

    private void OnDestroy()
    {
        ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);

        EventManager.RoundResultsReadyEvent -= OpenPopup;
    }

    public override void OpenPopup()
    {
        base.OpenPopup();
        if (roundSummary != null) roundSummary.Refresh();
    }

    public void OnContinueButtonClicked()
    {
        OnContinueButtonClickedEvent?.Invoke();
        ClosePopup();
    }
}
