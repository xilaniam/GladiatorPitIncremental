using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundSummaryUI : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] RoundStats roundStats;
    [SerializeField] EnemyManager enemyManager;
    [SerializeField] EnemyUnlockTracker unlockTracker;

    [Header("Header")]
    [SerializeField] TMP_Text roundNumberText;

    [Header("Animal Statistics")]
    [SerializeField] Transform rowContainer;
    [SerializeField] AnimalStatRow rowPrefab;
    [SerializeField] GameObject noKillsText;
    [SerializeField] int maxRows = 4;
    [SerializeField] Sprite otherAnimalsIcon;

    [Header("Round Rewards")]
    [SerializeField] TMP_Text animalEarningsText;
    [SerializeField] TMP_Text bonusText;
    [SerializeField] TMP_Text totalEarnedText;

    [Header("Next Animal Unlock")]
    [SerializeField] TMP_Text unlockHeaderText;
    [SerializeField] Image unlockPortrait;
    [SerializeField] TMP_Text unlockNameText;
    [SerializeField] TMP_Text unlockProgressText;
    [SerializeField] Image unlockProgressFill;

    private readonly List<AnimalStatRow> rows = new List<AnimalStatRow>();

    public void Refresh()
    {
        roundNumberText.text = $"ROUND {RoundManager.Instance.RoundNumber}";
        RefreshAnimalRows();
        RefreshRewards();
        RefreshNextUnlock();
    }

    void RefreshAnimalRows()
    {
        foreach (AnimalStatRow row in rows)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        rows.Clear();

        List<AnimalRoundTally> tallies = roundStats.Tallies.OrderByDescending(t => t.coins).ToList();
        noKillsText.SetActive(tallies.Count == 0);

        bool needsOtherRow = tallies.Count > maxRows;
        int shown = needsOtherRow ? maxRows - 1 : tallies.Count;

        foreach (AnimalRoundTally tally in tallies.Take(shown))
        {
            Enemy animal = enemyManager.GetPrefab(tally.type);
            AddRow(animal != null ? animal.Portrait : null, animal != null ? animal.DisplayName : tally.type.ToString(), tally.kills, tally.coins);
        }

        if (needsOtherRow)
        {
            var others = tallies.Skip(shown).ToList();
            AddRow(otherAnimalsIcon, "Other animals", others.Sum(t => t.kills), others.Sum(t => t.coins));
        }
    }

    void AddRow(Sprite icon, string animalName, int kills, int coins)
    {
        AnimalStatRow row = Instantiate(rowPrefab, rowContainer);
        row.Set(icon, animalName, kills, coins);
        rows.Add(row);
    }

    void RefreshRewards()
    {
        int earnings = roundStats.AnimalEarnings;
        int bonus = EconomyManager.Instance.LastRoundBonus;
        animalEarningsText.text = earnings.ToString("N0");
        bonusText.text = $"+{bonus:N0}";
        totalEarnedText.text = (earnings + bonus).ToString("N0");
    }

    void RefreshNextUnlock()
    {
        if (unlockTracker.UnlockedThisRound.Count > 0)
        {
            Enemy unlocked = enemyManager.GetPrefab(unlockTracker.UnlockedThisRound[unlockTracker.UnlockedThisRound.Count - 1]);
            ShowUnlock("NEW ANIMAL UNLOCKED!", unlocked, "Arrives next round!", 1f);
            return;
        }

        if (unlockTracker.TryGetNextUnlock(out EnemyUnlockRule rule))
        {
            Enemy next = enemyManager.GetPrefab(rule.unlocks);
            Enemy killType = enemyManager.GetPrefab(rule.killType);
            int kills = Mathf.Min(unlockTracker.GetKills(rule.killType), rule.killsRequired);
            string killName = killType != null ? killType.DisplayName : rule.killType.ToString();
            ShowUnlock("NEXT ANIMAL UNLOCK", next, $"{kills} / {rule.killsRequired} {killName} kills", (float)kills / rule.killsRequired);
            return;
        }

        ShowUnlock("ALL ANIMALS UNLOCKED", null, "", 1f);
    }

    void ShowUnlock(string header, Enemy animal, string progress, float fill)
    {
        unlockHeaderText.text = header;
        unlockPortrait.sprite = animal != null ? animal.Portrait : null;
        unlockPortrait.enabled = unlockPortrait.sprite != null;
        unlockNameText.text = animal != null ? animal.DisplayName : "";
        unlockProgressText.text = progress;
        unlockProgressFill.fillAmount = fill;
    }
}
