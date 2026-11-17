using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillManager : BaseUIPopup
{
    [SerializeField] AbilityManager abilityManager;
    [SerializeField] EnemyManager enemyManager;

    [SerializeField] Button ContinueButton;
    [SerializeField] TMP_Text balance;
    [SerializeField] SkillTooltip tooltip;

    private SkillContext context;
    private SkillNode[] nodes;

    public static event Action OnContinueButtonClickedEvent;
    public SkillContext Context => context;
    private void Start()
    {
        ClosePopup();
        context = new SkillContext(
        this,
        abilityManager,
        EconomyManager.Instance,
        enemyManager
         );

        nodes = GetComponentsInChildren<SkillNode>(true);
        foreach (SkillNode node in nodes)
        {
            node.OnNodeClickedAction += TrySkillUpgrade;
            node.OnHoverStartAction += tooltip.Show;
            node.OnHoverEndAction += tooltip.Hide;
        }

        ContinueButton.onClick.AddListener(OnContinueButtonClicked);
        RefreshSkillNodes();
    }

    private void OnDestroy()
    {
        ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
        if (nodes == null) return;
        foreach (SkillNode node in nodes)
        {
            node.OnNodeClickedAction -= TrySkillUpgrade;
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
        RefreshSkillNodes();
    }

    void TrySkillUpgrade(SkillNode skillNode)
    {
        if (!skillNode.CanUpgrade) return;

        int cost = skillNode.Cost;
        if (EconomyManager.Instance.Balance < cost)
            return;

        EconomyManager.Instance.DeductAmount(cost);

        skillNode.ApplyUpgrade(context);
        RefreshSkillNodes();
    }

    void RefreshSkillNodes()
    {
        if (nodes == null) return;

        balance.text = EconomyManager.Instance.Balance.ToString();
        foreach (SkillNode node in nodes)
        {
            node.Refresh();
        }
        tooltip.RefreshText();
    }
}
