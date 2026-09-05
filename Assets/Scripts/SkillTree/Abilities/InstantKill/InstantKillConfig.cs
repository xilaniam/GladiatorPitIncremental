using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Abilities/Instant Kill")]
public class InstantKillConfig : AbilityConfiguration
{
    [SerializeField, Range(0f, 1f)] private float chance = 0.05f;
    public float Chance => chance;

    public override IAbility CreateRuntime() => new InstantKillAbility(this);
}