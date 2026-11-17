using System;

namespace SMPro.SkillTree
{
    public enum StatFormat
    {
        Number,
        Percent
    }

    public enum ModifierKind
    {
        Flat,
        PercentAdd,
        Multiply,
        Override
    }

    [Serializable]
    public class StatDefinition
    {
        public string id;
        public string displayName;
        public float baseValue;
        public float min = float.MinValue;
        public float max = float.MaxValue;
        public bool isInteger;
        public StatFormat format;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
    }

    public readonly struct StatModifier
    {
        public ModifierKind Kind { get; }
        public float Value { get; }
        public object Source { get; }
        public float ExpiresAt { get; }

        public bool IsTemporary => !float.IsPositiveInfinity(ExpiresAt);

        public StatModifier(ModifierKind kind, float value, object source, float expiresAt = float.PositiveInfinity)
        {
            Kind = kind;
            Value = value;
            Source = source;
            ExpiresAt = expiresAt;
        }
    }
}
