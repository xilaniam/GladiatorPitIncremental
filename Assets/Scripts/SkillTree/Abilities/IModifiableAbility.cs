using UnityEngine;

public interface IModifiableAbility
{
    void Modify(AbilityStat stat, StatOperation operation, float value);
}

public enum AbilityStat
{
    Damage,
    Chance,
    ChainCount,
    Duration,
    Speed,
    Radius
}