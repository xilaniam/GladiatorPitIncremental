using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class BaseUIPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    public void OpenPopup()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void ClosePopup()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
