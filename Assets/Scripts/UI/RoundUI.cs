using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundUI : MonoBehaviour
{
    [SerializeField] TMP_Text timerText;
    [SerializeField] TextMeshProUGUI balance;

    private void Start()
    {
        balance.text = EconomyManager.Instance.Balance.ToString();
        EventManager.OnBalanceUpdatedEvent += UpdateBalanceUI;
    }

    private void OnDestroy()
    {
        EventManager.OnBalanceUpdatedEvent -= UpdateBalanceUI;
    }

    void UpdateBalanceUI(int value)
    {
        balance.text = value.ToString();
    }

    public void UpdateRoundTimerUI(float current , float max)
    {
        timerText.text = Mathf.CeilToInt(Mathf.Max(0f, max - current)).ToString();
    }
}
