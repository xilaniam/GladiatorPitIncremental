using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] List<SkillEffect> effects = new();
    [SerializeField] List<SkillNode> connectedNodes = new List<SkillNode>();
    [Header("References")]
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Image fillMeter;
    [SerializeField] LineRenderer connectorLinePrefab;
    [SerializeField] GameObject DetailsPanel;
    [SerializeField] TMP_Text description;
    [Header("Settings")]
    [SerializeField] int maxLevel = 3;
    [SerializeField] int requiredLevelToExpand = 1;
    [SerializeField] int initialCost = 2;
    [SerializeField] float costIncreasePerLevel = 0.25f;

    int currentLevel = 0;
    Button button;
    int currentCost;

    public int Cost => currentCost;
    public Action<SkillNode> OnNodeClickedAction;
    void Start()
    {
        button = GetComponent<Button>();
        currentCost = initialCost;

        button.onClick.AddListener(OnButtonClicked);

        if (DetailsPanel != null)
            DetailsPanel.SetActive(false);

        UpdateDescription();
    }

    void UpdateDescription()
    {
        description.text = "";
        foreach (SkillEffect effect in effects)
        {
            description.text += effect.GetDescription() + "\n" + "cost : " + currentCost + "\n" + currentLevel + "/" + maxLevel;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        DetailsPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DetailsPanel.SetActive(false);
    }

    public void SetNodeActive()
    {
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClicked);
    }

    public void SetUpgradeStatus(int balance)
    {
        //if (button == null) return;
        //if (balance < currentCost || currentLevel >= maxLevel)
        //{
        //    button.interactable = false;
        //}
        //else
        //{
        //    button.interactable = true;
        //}
    }

    void OnButtonClicked()
    {
        OnNodeClickedAction?.Invoke(this);
    }

    public void ApplyUpgrade(SkillContext context)
    {
        if (currentLevel >= maxLevel) return;

        currentLevel++;
        currentCost = Mathf.CeilToInt(initialCost * (1 + costIncreasePerLevel * currentLevel));

        fillMeter.fillAmount = (float)currentLevel / maxLevel;
        if (currentLevel == requiredLevelToExpand) ExpandSkillNodes();
        foreach (SkillEffect effect in effects)
        {
            if (effect == null) continue;
            effect.Apply(context);
        }
        UpdateDescription();
        //Here do what this skill does, like increasing damage, health, etc.
    }

    void ExpandSkillNodes()
    {
        if(connectedNodes.Count == 0) return;
        foreach (SkillNode node in connectedNodes)
        {
            CreateConnectionTo(node);
            node.SetNodeActive();    
        }
    }

    void CreateConnectionTo(SkillNode node)
    {
       LineRenderer lr = Instantiate(connectorLinePrefab, transform.parent);
       lr.positionCount = 2;
       lr.SetPosition(0, rectTransform.anchoredPosition);
       lr.SetPosition(1, node.rectTransform.anchoredPosition);
    }

    #region EditorHepler
#if UNITY_EDITOR
    [SerializeField, HideInInspector] string nodeGuid = "";
    private float autoConnectRadius = 260f;
    void OnValidate()
    {
        if (Application.isPlaying) return;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        RenameToFirstEffect();

        EditorApplication.delayCall += HandleEditorValidate;
    }

    void RenameToFirstEffect()
    {
        if (effects.Count == 0 || effects[0] == null) return;

        string desiredName = effects[0].name;
        if (gameObject.name != desiredName)
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

        if (!isFreshDuplicate) return; // already a known, wired-up node — leave it alone

        Undo.RecordObject(this, "Reset Duplicated Skill Node");
        nodeGuid = Guid.NewGuid().ToString();
        connectedNodes.Clear();
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

        if (nearestParent != null && !nearestParent.connectedNodes.Contains(this))
        {
            Undo.RecordObject(nearestParent, "Auto Connect Skill Node");
            nearestParent.connectedNodes.Add(this);
            EditorUtility.SetDirty(nearestParent);
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

            int removed = node.connectedNodes.RemoveAll(n => n == null);
            if (removed > 0)
            {
                Undo.RecordObject(node, "Clean Null Skill Node References");
                EditorUtility.SetDirty(node);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (connectedNodes == null || connectedNodes.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (var node in connectedNodes)
        {
            if (node == null) continue; // in case cleanup hasn't run yet
            Gizmos.DrawLine(transform.position, node.transform.position);
        }
    }
    void OnDrawGizmosSelected()
    {
        // Highlight this node and its direct connections when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (connectedNodes == null) return;
        foreach (var node in connectedNodes)
        {
            if (node == null) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, node.transform.position);
            Gizmos.DrawWireSphere(node.transform.position, 0.5f);
        }
    }
#endif
    #endregion
}
