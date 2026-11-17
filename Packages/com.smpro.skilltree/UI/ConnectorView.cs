using UnityEngine;
using UnityEngine.UI;

namespace SMPro.SkillTree.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ConnectorView : MonoBehaviour
    {
        [SerializeField] Graphic line;
        [SerializeField, Min(0.5f)] float thickness = 6f;
        [SerializeField, Tooltip("Gap left at each end so the line stops at the node's edge")] float endInset;

        public SkillNodeView From { get; private set; }
        public SkillNodeView To { get; private set; }
        public NodeRequirement Requirement { get; private set; }

        internal void Bind(SkillNodeView from, SkillNodeView to, NodeRequirement requirement)
        {
            From = from;
            To = to;
            Requirement = requirement;
            name = $"{from.Node.DisplayName} -> {to.Node.DisplayName}";
            Layout();
        }

        internal void Refresh(bool active, NodeStateStyle style)
        {
            bool visible = From.State != NodeState.Hidden && To.State != NodeState.Hidden;
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
            if (visible && line != null) line.color = active ? style.connectorActive : style.connectorInactive;
        }

        void Layout()
        {
            var rect = (RectTransform)transform;
            var parent = (RectTransform)rect.parent;
            Vector2 start = parent.InverseTransformPoint(From.RectTransform.position);
            Vector2 end = parent.InverseTransformPoint(To.RectTransform.position);
            Vector2 direction = end - start;
            float length = Mathf.Max(0f, direction.magnitude - endInset * 2f);

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(length, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            rect.localScale = Vector3.one;
        }

        void Reset()
        {
            line = GetComponent<Graphic>();
        }
    }
}
