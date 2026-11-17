using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimalStatRow : MonoBehaviour
{
    [SerializeField] Image portrait;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text killsText;
    [SerializeField] TMP_Text coinsText;

    public void Set(Sprite icon, string animalName, int kills, int coins)
    {
        portrait.sprite = icon;
        portrait.enabled = icon != null;
        nameText.text = animalName;
        killsText.text = kills == 1 ? "1 kill" : $"{kills:N0} kills";
        coinsText.text = coins.ToString("N0");
    }
}
