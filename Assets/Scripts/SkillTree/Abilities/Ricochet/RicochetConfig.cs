using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Abilities/Ricochet Spears")]
public class RicochetConfig : AbilityConfiguration
{
    [SerializeField, Tooltip("Random animals hit after each spear impact")]
    private int bounceCount = 1;

    [SerializeField, Range(0f, 2f), Tooltip("Share of the spear's damage dealt by each bounce")]
    private float bounceDamagePercent = 0.4f;

    [SerializeField, Range(0f, 2f), Tooltip("Share of the spear's damage dealt by the splinter hit (0 = no splinter)")]
    private float splinterDamagePercent = 0f;

    [Header("Visuals")]
    [SerializeField] private HomingProjectile bounceProjectile;
    [SerializeField] private HomingProjectile shardProjectile;

    public int BounceCount => bounceCount;
    public HomingProjectile BounceProjectile => bounceProjectile;
    public HomingProjectile ShardProjectile => shardProjectile;
    public float BounceDamagePercent => bounceDamagePercent;
    public float SplinterDamagePercent => splinterDamagePercent;

    public override IAbility CreateRuntime() => new RicochetAbility(this);
}
