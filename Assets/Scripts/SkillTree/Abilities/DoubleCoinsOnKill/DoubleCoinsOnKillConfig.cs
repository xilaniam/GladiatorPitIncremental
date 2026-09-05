using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Abilities/Double Coins On Kill")]
public class DoubleCoinsOnKillConfig : AbilityConfiguration
{
    [SerializeField, Range(0f, 1f)] private float chance = 0.15f;
    public float Chance => chance;

    public override IAbility CreateRuntime() => new DoubleCoinsOnKillAbility(this);
}
