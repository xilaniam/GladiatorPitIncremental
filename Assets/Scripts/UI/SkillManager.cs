using System;
using System.Collections.Generic;
using SMPro.SkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillManager : BaseUIPopup
{
    [SerializeField] SkillTreeController controller;
    [SerializeField] Button ContinueButton;
    [SerializeField] TMP_Text balance;
    [SerializeField] SkillTooltip tooltip;

    readonly Dictionary<string, SkillNode> nodesById = new();
    SkillNode[] nodes;
    bool isOpen;

    public static event Action OnContinueButtonClickedEvent;

    void Start()
    {
        ClosePopup();

        nodes = GetComponentsInChildren<SkillNode>(true);
        foreach (SkillNode node in nodes)
        {
            if (!node.Bind(controller, FindNode)) continue;
            nodesById[node.NodeId] = node;
            node.OnNodeClickedAction += TryPurchase;
            node.OnHoverStartAction += tooltip.Show;
            node.OnHoverEndAction += tooltip.Hide;
        }

        controller.TreeChanged += RefreshSkillNodes;
        if (controller.Wallet != null) controller.Wallet.BalanceChanged += OnBalanceChanged;
        ContinueButton.onClick.AddListener(OnContinueButtonClicked);
        RefreshSkillNodes();
    }

    void OnDestroy()
    {
        ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
        if (controller != null)
        {
            controller.TreeChanged -= RefreshSkillNodes;
            if (controller.Wallet != null) controller.Wallet.BalanceChanged -= OnBalanceChanged;
        }
        if (nodes == null) return;
        foreach (SkillNode node in nodes)
        {
            node.OnNodeClickedAction -= TryPurchase;
            node.OnHoverStartAction -= tooltip.Show;
            node.OnHoverEndAction -= tooltip.Hide;
        }
    }

    public void OnContinueButtonClicked()
    {
        OnContinueButtonClickedEvent?.Invoke();
        ClosePopup();
    }

    public override void OpenPopup()
    {
        base.OpenPopup();
        isOpen = true;
        RefreshSkillNodes();
    }

    public override void ClosePopup()
    {
        base.ClosePopup();
        isOpen = false;
    }

    SkillNode FindNode(string nodeId) => nodesById.TryGetValue(nodeId, out SkillNode node) ? node : null;

    void TryPurchase(SkillNode node)
    {
        if (controller.TryPurchase(node.NodeId)) node.PlayPurchasePulse();
    }

    void OnBalanceChanged(string currencyId)
    {
        if (isOpen) RefreshSkillNodes();
    }

    void RefreshSkillNodes()
    {
        if (nodes == null) return;

        balance.text = EconomyManager.Instance.Balance.ToString();
        foreach (SkillNode node in nodes) node.Refresh();
        tooltip.RefreshText();
    }
}
