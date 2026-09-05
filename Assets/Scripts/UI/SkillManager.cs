using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillManager : BaseUIPopup
{
    [SerializeField] AbilityManager abilityManager;
    [SerializeField] Button ContinueButton;
    [SerializeField] SkillNode rootSkillNode;
    [SerializeField] List<SkillNode> nodes = new List<SkillNode>();
    [SerializeField] TMP_Text balance;

    private SkillContext context;

    public SkillContext Context => context;
    private void Start()
    {
        context = new SkillContext(
        this,
        abilityManager,
        EconomyManager.Instance
         );

        foreach (SkillNode node in nodes)
        {
            node.OnNodeClickedAction += TrySkillUpgrade;
            node.gameObject.SetActive(false);
        }

        rootSkillNode.SetNodeActive();
        rootSkillNode.OnNodeClickedAction += TrySkillUpgrade;

        ContinueButton.onClick.AddListener(OnContinueButtonClicked);
        RefreshSkillNodes();
    }

    private void OnDestroy()
    {
        ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
        foreach (SkillNode node in nodes)
        {
            node.OnNodeClickedAction -= TrySkillUpgrade;
        }
        rootSkillNode.OnNodeClickedAction -= TrySkillUpgrade;
    }

    public void OnContinueButtonClicked()
    {
        RoundManager.Instance.StartRound();
        ClosePopup();
    }

    public override void OpenPopup()
    {
        base.OpenPopup();
        RefreshSkillNodes();
    }

    void TrySkillUpgrade(SkillNode skillNode)
    {
        Debug.Log("Trying to upgrade skill: " + skillNode.name);
        int balance = EconomyManager.Instance.Balance;

        if (balance < skillNode.Cost)
            return;

        EconomyManager.Instance.DeductAmount(skillNode.Cost);

        skillNode.ApplyUpgrade(context);
        RefreshSkillNodes();
    }

    void RefreshSkillNodes()
    {
        int balance = EconomyManager.Instance.Balance;
        this.balance.text = balance.ToString();
        foreach (SkillNode node in nodes)
        {
            if (node.isActiveAndEnabled)
                node.SetUpgradeStatus(balance);
        }
    }
}
