using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Effects/Modify Int Stat")]
public class ModifyIntStatEffect : SkillEffect
{
    public IntVariable targetStat;
    public StatOperation operation;
    public int amount;

    public override void Apply(SkillContext skillContext)
    {
        switch (operation)
        {
            case StatOperation.Add: targetStat.Value += amount; break;
            case StatOperation.Multiply: targetStat.Value *= amount; break;
            case StatOperation.Set: targetStat.Value = amount; break;
        }
    }

    public override string GetDescription() =>
        operation == StatOperation.Add ? $"+{amount} {targetStat.name}" : $"x{amount} {targetStat.name}";
}