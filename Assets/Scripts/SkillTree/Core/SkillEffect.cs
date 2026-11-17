using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    public abstract void Apply(SkillContext skillContext);
    public virtual string GetDescription() => "";
}

public enum StatOperation { Add, Multiply, Set , AddPercent}

public static class StatOperationExtensions
{
    public static float Apply(this StatOperation operation, float current, float value) => operation switch
    {
        StatOperation.Add => current + value,
        StatOperation.Multiply => current * value,
        StatOperation.Set => value,
        StatOperation.AddPercent => current + current * (value / 100f),
        _ => current
    };
}

