using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrimeTween;
using SMPro.SkillTree;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] string nodeId;
    [Header("References")]
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Image fillMeter;
    [SerializeField] LineRenderer connectorLinePrefab;
    [Header("Animation")]
    [SerializeField] float hoverScale = 1.2f;
    [SerializeField] float pressedScale = 0.8f;
    [SerializeField] float hoverDuration = 0.12f;
    [SerializeField] float pulseDuration = 0.3f;
    [Header("Mystery (keystones)")]
    [SerializeField] Sprite mysteryIcon;
    [SerializeField] Image iconImage;
    [SerializeField] GameObject mysteryGlow;
    [SerializeField, TextArea] string mysteryDescription = "A special power lies down this path.\nKeep upgrading this branch to reveal it.";

    SkillTreeController controller;
    SkillNodeDefinition definition;
    Func<string, SkillNode> findNode;
    Sprite revealedIcon;
    bool wasMystery;
    bool isHovered;
    Tween hoverTween;
    Sequence pulseSequence;
    readonly HashSet<string> connectedFrom = new();
    Button button;

    public Action<SkillNode> OnNodeClickedAction;
    public Action<SkillNode> OnHoverStartAction;
    public Action<SkillNode> OnHoverEndAction;

    public string NodeId => nodeId;
    public string TooltipText { get; private set; }
    public RectTransform RectTransform => rectTransform;

    float RestScale => isHovered ? hoverScale : 1f;

    Button Button
    {
        get
        {
            if (button == null) button = GetComponent<Button>();
            return button;
        }
    }

    void Awake()
    {
        Button.onClick.AddListener(OnButtonClicked);
    }

    void OnDestroy()
    {
        Button.onClick.RemoveListener(OnButtonClicked);
    }

    void OnDisable()
    {
        StopScaleAnimation();
        isHovered = false;
        transform.localScale = Vector3.one;
    }

    public bool Bind(SkillTreeController treeController, Func<string, SkillNode> nodeLookup)
    {
        controller = treeController;
        findNode = nodeLookup;
        if (controller.Definition.TryGetNode(nodeId, out definition)) return true;

        Debug.LogError($"{name}: node id '{nodeId}' is not in {controller.Definition.name}.", this);
        return false;
    }

    public void Refresh()
    {
        if (definition == null) return;

        NodeState state = controller.GetState(nodeId);
        gameObject.SetActive(state != NodeState.Hidden);
        if (state == NodeState.Hidden) return;

        DrawConnections();

        Button.interactable = state == NodeState.Available || state == NodeState.Mystery;
        fillMeter.fillAmount = (float)controller.GetLevel(nodeId) / definition.maxLevel;
        RefreshMystery(state);
        BuildTooltipText(state);
    }

    public void PlayPurchasePulse()
    {
        if (!isActiveAndEnabled) return;
        StopScaleAnimation();
        pulseSequence = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Scale(transform, pressedScale, pulseDuration * 0.3f, Ease.OutQuad))
            .Chain(Tween.Scale(transform, RestScale, pulseDuration * 0.7f, Ease.OutBack));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateScale(RestScale);
        OnHoverStartAction?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateScale(RestScale);
        OnHoverEndAction?.Invoke(this);
    }

    void RefreshMystery(NodeState state)
    {
        if (definition.reveal != RevealRule.MysteryUntilAvailable) return;

        if (revealedIcon == null && iconImage != null) revealedIcon = iconImage.sprite;

        bool mystery = state == NodeState.Mystery;
        if (iconImage != null) iconImage.sprite = mystery ? mysteryIcon : revealedIcon;
        if (mysteryGlow != null) mysteryGlow.SetActive(controller.GetLevel(nodeId) == 0);

        if (wasMystery && !mystery) PlayPurchasePulse();
        wasMystery = mystery;
    }

    void AnimateScale(float target)
    {
        StopScaleAnimation();
        hoverTween = Tween.Scale(transform, target, hoverDuration, Ease.OutQuad, useUnscaledTime: true);
    }

    void StopScaleAnimation()
    {
        if (hoverTween.isAlive) hoverTween.Stop();
        if (pulseSequence.isAlive) pulseSequence.Stop();
    }

    void OnButtonClicked()
    {
        OnNodeClickedAction?.Invoke(this);
    }

    void BuildTooltipText(NodeState state)
    {
        if (state == NodeState.Mystery)
        {
            TooltipText = "???\n" + mysteryDescription;
            return;
        }

        int level = controller.GetLevel(nodeId);
        var text = new StringBuilder();
        text.AppendLine(definition.DisplayName);

        if (!string.IsNullOrEmpty(definition.flavourText))
        {
            text.AppendLine(definition.flavourText);
        }
        else
        {
            foreach (ISkillEffect effect in definition.effects)
            {
                string line = effect?.Describe(controller.Definition, Mathf.Max(level, 1));
                if (!string.IsNullOrEmpty(line)) text.AppendLine(line);
            }
        }

        text.AppendLine(state == NodeState.Maxed ? "MAX" : $"Cost: {controller.GetCost(nodeId)}");
        text.Append($"Level {level}/{definition.maxLevel}");

        List<string> unmet = definition.requirements
            .Where(r => controller.GetLevel(r.nodeId) < r.level)
            .Select(r => $"{NodeName(r.nodeId)} Lv{r.level}")
            .ToList();
        if (unmet.Count > 0) text.Append("\nRequires: ").Append(string.Join(", ", unmet));

        TooltipText = text.ToString();
    }

    string NodeName(string id) => controller.Definition.TryGetNode(id, out SkillNodeDefinition node) ? node.DisplayName : id;

    void DrawConnections()
    {
        foreach (NodeRequirement requirement in definition.requirements)
        {
            if (connectedFrom.Contains(requirement.nodeId)) continue;
            if (controller.GetState(requirement.nodeId) == NodeState.Hidden) continue;

            SkillNode parent = findNode(requirement.nodeId);
            if (parent == null) continue;

            connectedFrom.Add(requirement.nodeId);
            LineRenderer line = Instantiate(connectorLinePrefab, transform.parent);
            line.positionCount = 2;
            line.SetPosition(0, parent.RectTransform.anchoredPosition);
            line.SetPosition(1, rectTransform.anchoredPosition);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }
#endif
}
