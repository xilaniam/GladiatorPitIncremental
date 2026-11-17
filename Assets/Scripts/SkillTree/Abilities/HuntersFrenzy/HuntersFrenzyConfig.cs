using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Abilities/Hunter's Frenzy")]
public class HuntersFrenzyConfig : AbilityConfiguration
{
    [Header("Trigger")]
    [SerializeField, Tooltip("Every Nth kill in a round starts a Frenzy")]
    private int killsToTrigger = 25;
    [SerializeField] private int minKillsToTrigger = 10;
    [SerializeField] private float duration = 5f;

    [Header("While Active")]
    [SerializeField] private float attackSpeedMultiplier = 2f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField, Tooltip("Extra coins while active (0.5 = +50%)")]
    private float coinBonus = 0.5f;

    [Header("Stats Boosted While Active")]
    [SerializeField] private FloatVariable attackInterval;
    [SerializeField] private FloatVariable spearDamageMultiplier;

    public int KillsToTrigger => killsToTrigger;
    public int MinKillsToTrigger => minKillsToTrigger;
    public float Duration => duration;
    public float AttackSpeedMultiplier => attackSpeedMultiplier;
    public float DamageMultiplier => damageMultiplier;
    public float CoinBonus => coinBonus;
    public FloatVariable AttackInterval => attackInterval;
    public FloatVariable SpearDamageMultiplier => spearDamageMultiplier;

    public override IAbility CreateRuntime() => new HuntersFrenzyAbility(this);
}
