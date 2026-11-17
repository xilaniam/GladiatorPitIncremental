using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Abilities/Breeding Season")]
public class BreedingSeasonConfig : AbilityConfiguration
{
    [SerializeField, Range(0f, 1f), Tooltip("Chance that a kill brings in a new animal")]
    private float chance = 0.15f;

    public float Chance => chance;

    public override IAbility CreateRuntime() => new BreedingSeasonAbility(this);
}
