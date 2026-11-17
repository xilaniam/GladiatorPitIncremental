using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SkillRequirement
{
    public SkillNode node;
    public int level = 1;

    public bool IsMet => node == null || node.Level >= level;
}

[RequireComponent(typeof(Button))]
public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] string displayName;
    [SerializeField, TextArea] string effectDescription;
    [SerializeField] List<SkillEffect> effects = new();
    [SerializeField] List<SkillRequirement> requirements = new();
    [Header("References")]
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Image fillMeter;
    [SerializeField] LineRenderer connectorLinePrefab;
    [Header("Settings")]
    [SerializeField] int maxLevel = 3;
    [FormerlySerializedAs("initialCost")]
    [SerializeField] int baseCost = 2;
    [FormerlySerializedAs("costIncreasePerLevel")]
    [SerializeField] float costGrowth = 1.5f;
    [Header("Animation")]
    [SerializeField] float hoverScale = 1.2f;
    [SerializeField] float pressedScale = 0.8f;
    [SerializeField] float hoverDuration = 0.12f;
    [SerializeField] float pulseDuration = 0.3f;
    [Header("Mystery (keystones)")]
    [SerializeField, Tooltip("Shown from the start as a '?' until all requirements are met")] bool isMystery;
    [SerializeField] Sprite mysteryIcon;
    [SerializeField] Image iconImage;
    [SerializeField] GameObject mysteryGlow;
    [SerializeField, TextArea] string mysteryDescription = "A special power lies down this path.\nKeep upgrading this branch to reveal it.";

    int currentLevel;
    Sprite revealedIcon;
    bool wasHiddenMystery;
    bool isHovered;
    Tween hoverTween;
    Sequence pulseSequence;
    readonly HashSet<SkillNode> connectedFrom = new();
    Button button;

    public Action<SkillNode> OnNodeClickedAction;
    public Action<SkillNode> OnHoverStartAction;
    public Action<SkillNode> OnHoverEndAction;

    public string TooltipText { get; private set; }
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public int Level => currentLevel;
    public int Cost => Mathf.CeilToInt(baseCost * Mathf.Pow(costGrowth, currentLevel));
    public bool IsMaxLevel => currentLevel >= maxLevel;
    public bool RequirementsMet => requirements.All(r => r.IsMet);
    public bool CanUpgrade => !IsMaxLevel && RequirementsMet;

    public bool IsRevealed => isMystery || requirements.Count == 0 || requirements.Any(r => r.node != null && r.node.Level > 0);

    bool IsHiddenMystery => isMystery && currentLevel == 0 && !RequirementsMet;

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

    private void OnDestroy()
    {
        Button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        StopScaleAnimation();
        isHovered = false;
        transform.localScale = Vector3.one;
    }

    public void Refresh()
    {
        bool revealed = IsRevealed;
        gameObject.SetActive(revealed);
        if (!revealed) return;

        DrawConnections();

        Button.interactable = RequirementsMet || IsHiddenMystery;
        fillMeter.fillAmount = (float)currentLevel / maxLevel;
        RefreshMystery();
        BuildTooltipText();
    }

    void RefreshMystery()
    {
        if (!isMystery) return;

        if (revealedIcon == null && iconImage != null) revealedIcon = iconImage.sprite;

        bool hidden = IsHiddenMystery;
        if (iconImage != null) iconImage.sprite = hidden ? mysteryIcon : revealedIcon;
        if (mysteryGlow != null) mysteryGlow.SetActive(currentLevel == 0);

        if (wasHiddenMystery && !hidden) PlayPurchasePulse();
        wasHiddenMystery = hidden;
    }

    public void ApplyUpgrade(SkillContext context)
    {
        if (!CanUpgrade) return;

        currentLevel++;
        foreach (SkillEffect effect in effects)
        {
            if (effect == null) continue;
            effect.Apply(context);
        }
        PlayPurchasePulse();
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

    void AnimateScale(float target)
    {
        StopScaleAnimation();
        hoverTween = Tween.Scale(transform, target, hoverDuration, Ease.OutQuad, useUnscaledTime: true);
    }

    void PlayPurchasePulse()
    {
        StopScaleAnimation();
        pulseSequence = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Scale(transform, pressedScale, pulseDuration * 0.3f, Ease.OutQuad))
            .Chain(Tween.Scale(transform, RestScale, pulseDuration * 0.7f, Ease.OutBack));
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

    void BuildTooltipText()
    {
        if (IsHiddenMystery)
        {
            TooltipText = "???\n" + mysteryDescription;
            return;
        }

        StringBuilder text = new StringBuilder();
        text.AppendLine(DisplayName);

        if (!string.IsNullOrEmpty(effectDescription))
        {
            text.AppendLine(effectDescription);
        }
        else
        {
            foreach (SkillEffect effect in effects)
            {
                if (effect != null) text.AppendLine(effect.GetDescription());
            }
        }

        text.AppendLine(IsMaxLevel ? "MAX" : $"Cost: {Cost}");
        text.Append($"Level {currentLevel}/{maxLevel}");

        var unmet = requirements.Where(r => !r.IsMet).Select(r => $"{r.node.DisplayName} Lv{r.level}").ToList();
        if (unmet.Count > 0)
            text.Append("\nRequires: ").Append(string.Join(", ", unmet));

        TooltipText = text.ToString();
    }

    void DrawConnections()
    {
        foreach (SkillRequirement requirement in requirements)
        {
            SkillNode parent = requirement.node;
            if (parent == null || !parent.IsRevealed) continue;
            if (connectedFrom.Add(parent)) CreateConnectionFrom(parent);
        }
    }

    void CreateConnectionFrom(SkillNode node)
    {
        LineRenderer lr = Instantiate(connectorLinePrefab, transform.parent);
        lr.positionCount = 2;
        lr.SetPosition(0, node.rectTransform.anchoredPosition);
        lr.SetPosition(1, rectTransform.anchoredPosition);
    }

    #region EditorHepler
#if UNITY_EDITOR
    [SerializeField, HideInInspector] string nodeGuid = "";
    private float autoConnectRadius = 260f;
    void OnValidate()
    {
        if (Application.isPlaying) return;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        RenameToDisplayName();

        EditorApplication.delayCall += HandleEditorValidate;
    }

    void RenameToDisplayName()
    {
        string desiredName = displayName;
        if (string.IsNullOrEmpty(desiredName) && effects.Count > 0 && effects[0] != null)
            desiredName = effects[0].name;

        if (!string.IsNullOrEmpty(desiredName) && gameObject.name != desiredName)
        {
            gameObject.name = desiredName;
        }
    }

    void HandleEditorValidate()
    {
        if (this == null) return; // destroyed before delayCall fired

        SkillNode[] allNodes = transform.parent != null
            ? transform.parent.GetComponentsInChildren<SkillNode>(true)
            : FindObjectsByType<SkillNode>(FindObjectsSortMode.None);

        bool isFreshDuplicate = string.IsNullOrEmpty(nodeGuid)
            || allNodes.Any(n => n != this && n.nodeGuid == nodeGuid);

        if (!isFreshDuplicate) return; // already a known, wired-up node - leave it alone

        Undo.RecordObject(this, "Reset Duplicated Skill Node");
        nodeGuid = Guid.NewGuid().ToString();
        requirements.Clear();
        EditorUtility.SetDirty(this);

        TryAutoConnectToNearestParent(allNodes);
    }

    void TryAutoConnectToNearestParent(SkillNode[] allNodes)
    {
        SkillNode nearestParent = null;
        float nearestDist = float.MaxValue;

        foreach (var other in allNodes)
        {
            if (other == this) continue;
            float dist = Vector2.Distance(
                rectTransform.anchoredPosition,
                other.rectTransform.anchoredPosition);

            if (dist < nearestDist && dist <= autoConnectRadius)
            {
                nearestDist = dist;
                nearestParent = other;
            }
        }

        if (nearestParent != null)
        {
            Undo.RecordObject(this, "Auto Connect Skill Node");
            requirements.Add(new SkillRequirement { node = nearestParent, level = 1 });
            EditorUtility.SetDirty(this);
        }
    }

    static bool hierarchyHookRegistered = false;

    void OnEnable()
    {
        if (Application.isPlaying) return;
        if (!hierarchyHookRegistered)
        {
            EditorApplication.hierarchyChanged += CleanupAllNullReferences;
            hierarchyHookRegistered = true;
        }
    }

    static void CleanupAllNullReferences()
    {
        SkillNode[] allNodes = FindObjectsByType<SkillNode>(FindObjectsSortMode.None);
        foreach (var node in allNodes)
        {
            if (node == null) continue;

            int removed = node.requirements.RemoveAll(r => r.node == null);
            if (removed > 0)
            {
                Undo.RecordObject(node, "Clean Null Skill Node References");
                EditorUtility.SetDirty(node);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (requirements == null || requirements.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (var requirement in requirements)
        {
            if (requirement.node == null) continue; // in case cleanup hasn't run yet
            Gizmos.DrawLine(requirement.node.transform.position, transform.position);
        }
    }
    void OnDrawGizmosSelected()
    {
        // Highlight this node and the nodes it requires
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (requirements == null) return;
        foreach (var requirement in requirements)
        {
            if (requirement.node == null) continue;
            Gizmos.DrawLine(requirement.node.transform.position, transform.position);
            Gizmos.DrawWireSphere(requirement.node.transform.position, 0.5f);
        }
    }
#endif
    #endregion
}
