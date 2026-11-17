using SMPro.SkillTree;
using UnityEngine;

public class DoubleCoinsFeature : SkillFeature
{
    [SerializeField] EconomyManager economy;
    [SerializeField] StatRef chance;

    protected override void OnUnlocked() => economy.RegisterCoinModifier(ApplyDoubleChance);

    protected override void OnLocked()
    {
        if (economy != null) economy.UnregisterCoinModifier(ApplyDoubleChance);
    }

    float ApplyDoubleChance(float coins) => Random.value < chance.Value ? coins * 2f : coins;
}
