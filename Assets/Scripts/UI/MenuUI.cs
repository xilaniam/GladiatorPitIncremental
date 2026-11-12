using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : BaseUIPopup
{
    [SerializeField] Button ContinueButton;

    public static event Action OnContinueButtonClickedEvent;
    private void Start()
    {
        ContinueButton.onClick.AddListener(OnContinueButtonClicked);

        EventManager.RoundEndedEvent += OpenPopup;
    }

    private void OnDestroy()
    {
        ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);

        EventManager.RoundEndedEvent -= OpenPopup;
    }

    public void OnContinueButtonClicked()
    {
        OnContinueButtonClickedEvent?.Invoke();
        ClosePopup();
    }
}
