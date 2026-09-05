using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    public abstract void Apply(SkillContext skillContext);
    public virtual string GetDescription() => "";
}

public enum StatOperation { Add, Multiply, Set }

