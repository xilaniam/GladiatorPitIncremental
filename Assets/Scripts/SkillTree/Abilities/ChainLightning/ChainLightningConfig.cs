using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Abilities/Chain Lightning")]
public class ChainLightningConfig : AbilityConfiguration
{
    [Header("Base Stats")]

    [SerializeField, Range(0f, 1f), Tooltip("Share of the spear's damage dealt by each jump")]
    private float damagePercent = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float chance = 0.1f;

    [SerializeField]
    private int chainCount = 2;

    public float DamagePercent => damagePercent;
    public float Chance => chance;
    public int ChainCount => chainCount;

    public override IAbility CreateRuntime()
    {
        return new ChainLightningAbility(this);
    }
}
