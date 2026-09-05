using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Abilities/Chain Lightning")]
public class ChainLightningConfig : AbilityConfiguration
{
    [Header("Base Stats")]

    [SerializeField]
    private float damage = 10f;

    [SerializeField, Range(0f, 1f)]
    private float chance = 0.2f;

    [SerializeField]
    private int chainCount = 3;

    public float Damage => damage;
    public float Chance => chance;
    public int ChainCount => chainCount;

    public override IAbility CreateRuntime()
    {
        return new ChainLightningAbility(this);
    }
}
