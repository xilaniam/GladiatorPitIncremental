using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTreePanZoom : MonoBehaviour, IScrollHandler, IDragHandler, IBeginDragHandler
{
    [SerializeField] RectTransform content;      
    [SerializeField] RectTransform viewport;      

    [SerializeField] float zoomSpeed = 0.1f;
    [SerializeField] float minZoom = 0.5f;
    [SerializeField] float maxZoom = 2f;

    Vector2 lastDragPos;

    public void OnScroll(PointerEventData eventData)
    {
        float zoomDelta = eventData.scrollDelta.y * zoomSpeed;
        float newScale = Mathf.Clamp(content.localScale.x + zoomDelta, minZoom, maxZoom);

        Vector2 localCursorBefore;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content, eventData.position, eventData.pressEventCamera, out localCursorBefore);

        content.localScale = Vector3.one * newScale;

        Vector2 localCursorAfter;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content, eventData.position, eventData.pressEventCamera, out localCursorAfter);

        content.anchoredPosition += (localCursorAfter - localCursorBefore) * newScale;

        ClampToBounds();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastDragPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - lastDragPos;
        content.anchoredPosition += delta;
        lastDragPos = eventData.position;

        ClampToBounds();
    }

    void ClampToBounds()
    {
        // Optional: prevent panning the tree completely off-screen.
        // Tune these limits to your tree's actual size.
        float maxOffset = 1500f * content.localScale.x;
        content.anchoredPosition = new Vector2(
            Mathf.Clamp(content.anchoredPosition.x, -maxOffset, maxOffset),
            Mathf.Clamp(content.anchoredPosition.y, -maxOffset, maxOffset));
    }
}