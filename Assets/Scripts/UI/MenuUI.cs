using UnityEngine;
using UnityEngine.UI;

public class MenuUI : BaseUIPopup
{
    [SerializeField] Button ContinueButton;

    private void Start()
    {
        ContinueButton.onClick.AddListener(OnContinueButtonClicked);

        RoundManager.Instance.RoundEndedEvent += OpenPopup;
    }

    private void OnDestroy()
    {
        ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);

        RoundManager.Instance.RoundEndedEvent -= OpenPopup;
    }

    public void OnContinueButtonClicked()
    {
        RoundManager.Instance.StartRound();
        ClosePopup();
    }
}
