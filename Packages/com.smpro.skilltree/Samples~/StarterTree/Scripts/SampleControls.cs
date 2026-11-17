using SMPro.SkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SMPro.SkillTree.Samples
{
    public class SampleControls : MonoBehaviour
    {
        [SerializeField] SkillTreeController controller;
        [SerializeField] SimpleWallet wallet;
        [SerializeField] TMP_Text goldLabel;
        [SerializeField] Button addGoldButton;
        [SerializeField] Button resetButton;
        [SerializeField] long goldPerClick = 100;

        void OnEnable()
        {
            wallet.BalanceChanged += OnBalanceChanged;
            addGoldButton.onClick.AddListener(AddGold);
            resetButton.onClick.AddListener(ResetTree);
            UpdateLabel();
        }

        void OnDisable()
        {
            wallet.BalanceChanged -= OnBalanceChanged;
            addGoldButton.onClick.RemoveListener(AddGold);
            resetButton.onClick.RemoveListener(ResetTree);
        }

        void AddGold() => wallet.Add(CurrencyIds.Default, goldPerClick);

        void ResetTree() => controller.ResetTree();

        void OnBalanceChanged(string currencyId) => UpdateLabel();

        void UpdateLabel() => goldLabel.text = $"Gold: {wallet.GetBalance(CurrencyIds.Default)}";
    }
}
