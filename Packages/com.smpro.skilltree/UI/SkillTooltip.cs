using TMPro;
using UnityEngine;

namespace SMPro.SkillTree.UI
{
    public class SkillTooltip : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] TMP_Text text;
        [SerializeField, Tooltip("Gap between the node and the tooltip")] float gap = 16f;
        [SerializeField, Tooltip("Keeps the tooltip this far from the edges of its area")] float edgePadding = 12f;

        RectTransform area;
        Camera canvasCamera;
        RectTransform target;

        public RectTransform Target => target;

        void Awake()
        {
            area = (RectTransform)panel.parent;
            Canvas canvas = panel.GetComponentInParent<Canvas>(true).rootCanvas;
            canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            panel.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            target = null;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public void Show(RectTransform node, string content)
        {
            target = node;
            text.text = content;
            panel.gameObject.SetActive(true);
            UpdatePosition();
        }

        public void SetText(string content)
        {
            if (target != null) text.text = content;
        }

        public void Hide(RectTransform node)
        {
            if (target != node) return;
            target = null;
            panel.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (!target.gameObject.activeInHierarchy)
            {
                Hide(target);
                return;
            }
            UpdatePosition();
        }

        void UpdatePosition()
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvasCamera, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, canvasCamera, out Vector2 nodeCenter);

            float nodeHalfHeight = target.rect.height * 0.5f * target.lossyScale.y / area.lossyScale.y;
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
}
