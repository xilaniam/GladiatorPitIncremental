using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SMPro.SkillTree.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SkillNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Parts")]
        [SerializeField] Image icon;
        [SerializeField] Image levelFill;
        [SerializeField] TMP_Text levelText;
        [SerializeField, Tooltip("Optional. Tinted by the style for each state")] Graphic stateTint;
        [SerializeField, Tooltip("Optional. Shown while the node has the style's highlight tag and is not bought")] GameObject highlight;
        [SerializeField, Tooltip("Optional. Shown while the node can be bought right now")] GameObject affordableIndicator;
        [SerializeField, Tooltip("Optional. Its interactable flag follows the node state, so its colour tint dims locked nodes")] Button button;

        [Header("Animation")]
        [SerializeField] float hoverScale = 1.2f;
        [SerializeField] float pressedScale = 0.8f;
        [SerializeField] float hoverDuration = 0.12f;
        [SerializeField] float pulseDuration = 0.3f;

        SkillTreeView owner;
        bool isHovered;
        bool wasMystery;
        Tween hoverTween;
        Sequence pulseSequence;

        public SkillNodeDefinition Node { get; private set; }
        public string NodeId => Node?.id;
        public NodeState State { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;

        float RestScale => isHovered ? hoverScale : 1f;

        internal void Bind(SkillTreeView view, SkillNodeDefinition node)
        {
            owner = view;
            Node = node;
            name = node.DisplayName;
        }

        internal void Refresh(NodeState state, int level, bool canPurchase, NodeStateStyle style)
        {
            State = state;
            bool visible = state != NodeState.Hidden;
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
            if (!visible) return;

            bool mystery = state == NodeState.Mystery;
            if (icon != null) icon.sprite = mystery && style.mysteryIcon != null ? style.mysteryIcon : Node.icon;
            if (levelFill != null) levelFill.fillAmount = (float)level / Node.maxLevel;
            if (levelText != null) levelText.text = $"{level}/{Node.maxLevel}";
            if (stateTint != null) stateTint.color = style.TintFor(state);
            if (button != null) button.interactable = state == NodeState.Available || mystery;
            if (highlight != null) highlight.SetActive(level == 0 && !string.IsNullOrEmpty(style.highlightTag) && Node.HasTag(style.highlightTag));
            if (affordableIndicator != null) affordableIndicator.SetActive(canPurchase);

            if (wasMystery && !mystery) PlayPurchasePulse();
            wasMystery = mystery;
        }

        public void PlayPurchasePulse()
        {
            if (!isActiveAndEnabled) return;
            StopAnimation();
            pulseSequence = Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.Scale(transform, pressedScale, pulseDuration * 0.3f, Ease.OutQuad))
                .Chain(Tween.Scale(transform, RestScale, pulseDuration * 0.7f, Ease.OutBack));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            AnimateScale(RestScale);
            owner.HandleHover(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            AnimateScale(RestScale);
            owner.HandleHover(this, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) owner.HandleClick(this);
        }

        void OnDisable()
        {
            StopAnimation();
            isHovered = false;
            transform.localScale = Vector3.one;
            if (owner != null) owner.HandleHover(this, false);
        }

        void AnimateScale(float target)
        {
            if (!isActiveAndEnabled) return;
            StopAnimation();
            hoverTween = Tween.Scale(transform, target, hoverDuration, Ease.OutQuad, useUnscaledTime: true);
        }

        void StopAnimation()
        {
            if (hoverTween.isAlive) hoverTween.Stop();
            if (pulseSequence.isAlive) pulseSequence.Stop();
        }
    }
}
