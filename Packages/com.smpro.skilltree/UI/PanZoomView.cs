using UnityEngine;
using UnityEngine.EventSystems;

namespace SMPro.SkillTree.UI
{
    public class PanZoomView : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] RectTransform content;
        [SerializeField] RectTransform viewport;

        [Header("Zoom")]
        [SerializeField, Tooltip("Zoom change per scroll notch (0.15 = 15%)")] float zoomStep = 0.15f;
        [SerializeField, Tooltip("Higher = snappier, lower = floatier")] float zoomSmoothing = 12f;
        [SerializeField] float minZoom = 0.5f;
        [SerializeField] float maxZoom = 2f;

        [Header("Pan")]
        [SerializeField] float panLimit = 1500f;
        [SerializeField, Tooltip("How quickly the glide after a drag slows down")] float inertiaDamping = 6f;
        [SerializeField, Tooltip("A drag that stopped for this long before release does not glide")] float inertiaReleaseWindow = 0.05f;

        RectTransform contentParent;
        Camera canvasCamera;
        float targetZoom = 1f;
        Vector2 zoomFocus;
        Vector2 lastDragPoint;
        Vector2 velocity;
        float lastDragTime;
        bool isDragging;

        void Awake()
        {
            contentParent = (RectTransform)content.parent;
            Canvas canvas = viewport.GetComponentInParent<Canvas>(true).rootCanvas;
            canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        void OnEnable()
        {
            targetZoom = content.localScale.x;
            velocity = Vector2.zero;
            isDragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!TryGetParentPoint(eventData.position, out zoomFocus)) return;
            targetZoom = Mathf.Clamp(targetZoom * Mathf.Pow(1f + zoomStep, eventData.scrollDelta.y), minZoom, maxZoom);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = TryGetParentPoint(eventData.position, out lastDragPoint);
            velocity = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || !TryGetParentPoint(eventData.position, out Vector2 point)) return;

            Vector2 delta = point - lastDragPoint;
            lastDragPoint = point;
            content.anchoredPosition += delta;

            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            velocity = Vector2.Lerp(velocity, delta / dt, 0.5f);
            lastDragTime = Time.unscaledTime;
            ClampToBounds();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            if (Time.unscaledTime - lastDragTime > inertiaReleaseWindow) velocity = Vector2.zero;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateZoom(dt);
            UpdateInertia(dt);
            ClampToBounds();
        }

        void UpdateZoom(float dt)
        {
            float current = content.localScale.x;
            if (Mathf.Approximately(current, targetZoom)) return;

            float next = Mathf.Lerp(current, targetZoom, 1f - Mathf.Exp(-zoomSmoothing * dt));
            if (Mathf.Abs(next - targetZoom) < 0.0005f) next = targetZoom;

            Vector2 pivot = content.localPosition;
            content.localPosition = zoomFocus - (zoomFocus - pivot) * (next / current);
            content.localScale = Vector3.one * next;
        }

        void UpdateInertia(float dt)
        {
            if (isDragging || velocity.sqrMagnitude < 1f) return;

            content.anchoredPosition += velocity * dt;
            velocity *= Mathf.Exp(-inertiaDamping * dt);
        }

        void ClampToBounds()
        {
            float maxOffset = panLimit * content.localScale.x;
            content.anchoredPosition = new Vector2(
                Mathf.Clamp(content.anchoredPosition.x, -maxOffset, maxOffset),
                Mathf.Clamp(content.anchoredPosition.y, -maxOffset, maxOffset));
        }

        bool TryGetParentPoint(Vector2 screenPosition, out Vector2 point) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(contentParent, screenPosition, canvasCamera, out point);
    }
}
