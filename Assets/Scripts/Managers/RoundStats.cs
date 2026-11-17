using System.Collections.Generic;
using System.Linq;

public class AnimalRoundTally
{
    public AnimalType type;
    public int kills;
    public int coins;
}

public class RoundStats : GameEventListener
{
    private readonly Dictionary<AnimalType, AnimalRoundTally> tallies = new Dictionary<AnimalType, AnimalRoundTally>();

    public IEnumerable<AnimalRoundTally> Tallies => tallies.Values;
    public int TotalKills => tallies.Values.Sum(t => t.kills);
    public int AnimalEarnings => tallies.Values.Sum(t => t.coins);

    private void Start()
    {
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    protected override void SubscribeEvents()
    {
        EventManager.RoundStartedEvent += ResetTallies;
        EventManager.OnCoinsEarnedEvent += RecordKill;
    }

    protected override void UnsubscribeEvents()
    {
        EventManager.RoundStartedEvent -= ResetTallies;
        EventManager.OnCoinsEarnedEvent -= RecordKill;
    }

    void ResetTallies() => tallies.Clear();

    void RecordKill(Enemy enemy, int coins)
    {
        if (!tallies.TryGetValue(enemy.AnimalType, out AnimalRoundTally tally))
        {
            tally = new AnimalRoundTally { type = enemy.AnimalType };
            tallies.Add(enemy.AnimalType, tally);
        }

        tally.kills++;
        tally.coins += coins;
    }
}
