using TMPro;
using UnityEngine;

public class SkillTooltip : MonoBehaviour
{
    [SerializeField] RectTransform panel;
    [SerializeField] TMP_Text text;
    [SerializeField, Tooltip("Gap between the node and the tooltip")] float gap = 16f;
    [SerializeField, Tooltip("Keeps the tooltip this far from the screen edges")] float edgePadding = 12f;

    private RectTransform area;
    private Camera canvasCamera;
    private SkillNode target;

    private void Awake()
    {
        area = (RectTransform)panel.parent;
        Canvas canvas = panel.GetComponentInParent<Canvas>(true).rootCanvas;
        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        panel.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        target = null;
        panel.gameObject.SetActive(false);
    }

    public void Show(SkillNode node)
    {
        target = node;
        text.text = node.TooltipText;
        panel.gameObject.SetActive(true);
        UpdatePosition();
    }

    public void Hide(SkillNode node)
    {
        if (target != node) return;
        target = null;
        panel.gameObject.SetActive(false);
    }

    public void RefreshText()
    {
        if (target != null) text.text = target.TooltipText;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (!target.isActiveAndEnabled) { Hide(target); return; }
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        RectTransform node = (RectTransform)target.transform;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvasCamera, node.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, canvasCamera, out Vector2 nodeCenter);

        float nodeHalfHeight = node.rect.height * 0.5f * node.lossyScale.y / area.lossyScale.y;
        Vector2 size = panel.rect.size;
        Rect bounds = area.rect;

        float above = nodeCenter.y + nodeHalfHeight + gap + size.y * 0.5f;
        float below = nodeCenter.y - nodeHalfHeight - gap - size.y * 0.5f;
        float y = above + size.y * 0.5f <= bounds.yMax - edgePadding ? above : below;

        float halfWidth = size.x * 0.5f;
        float x = Mathf.Clamp(nodeCenter.x, bounds.xMin + halfWidth + edgePadding, bounds.xMax - halfWidth - edgePadding);

        panel.localPosition = new Vector2(x, y);
    }
}
