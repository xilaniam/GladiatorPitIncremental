using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Abilities/Spawn Enemies At Interval")]
public class EnemySpawnIntervalConfig : AbilityConfiguration
{
    [SerializeField] float spawnInterval = 5f;

    public float SpawnInterval => spawnInterval;
    public override IAbility CreateRuntime()
    {
        return new EnemySpawnIntervalAbility(this);
    }
}
