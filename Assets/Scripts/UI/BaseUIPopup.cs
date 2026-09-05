using UnityEngine;


public class BaseUIPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject panel;
    [SerializeField] bool useInactive;
    public virtual void OpenPopup()
    {
        if(useInactive) panel.SetActive(true);
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public virtual void ClosePopup()
    {
        if(useInactive) panel.SetActive(false);
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
