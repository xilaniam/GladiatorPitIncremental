using UnityEngine;
using UnityEngine.UI;

public class SkillTreeUI : BaseUIPopup
{
    [SerializeField] Button ContinueButton;

    private void Start()
    {
        ContinueButton.onClick.AddListener(OnContinueButtonClicked);
    }

    private void OnDestroy()
    {
        ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
    }

    public void OnContinueButtonClicked()
    {
        RoundManager.Instance.StartRound();
        ClosePopup();
    }
}
