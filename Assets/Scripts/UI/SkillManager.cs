using System;
using SMPro.SkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillManager : BaseUIPopup
{
    [SerializeField] SkillTreeController controller;
    [SerializeField] Button ContinueButton;
    [SerializeField] TMP_Text balance;

    public static event Action OnContinueButtonClickedEvent;

    void Start()
    {
        ClosePopup();
        if (controller.Wallet != null) controller.Wallet.BalanceChanged += OnBalanceChanged;
        ContinueButton.onClick.AddListener(OnContinueButtonClicked);
        UpdateBalance();
    }

    void OnDestroy()
    {
        ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
        if (controller != null && controller.Wallet != null) controller.Wallet.BalanceChanged -= OnBalanceChanged;
    }

    public void OnContinueButtonClicked()
    {
        OnContinueButtonClickedEvent?.Invoke();
        ClosePopup();
    }

    public override void OpenPopup()
    {
        base.OpenPopup();
        UpdateBalance();
    }

    void OnBalanceChanged(string currencyId) => UpdateBalance();

    void UpdateBalance() => balance.text = EconomyManager.Instance.Balance.ToString();
}
