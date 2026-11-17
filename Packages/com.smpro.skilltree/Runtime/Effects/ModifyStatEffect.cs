using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree
{
    [Serializable, SkillEffectMenu("Modify Stat")]
    public class ModifyStatEffect : ISkillEffect
    {
        [StatId] public string statId;
        public ModifierKind kind = ModifierKind.Flat;
        public float valuePerLevel;
        [Tooltip("Optional exact value for each level. When set, Value Per Level is ignored.")]
        public float[] valueByLevel = Array.Empty<float>();

        public float ValueAt(int level)
        {
            if (valueByLevel != null && valueByLevel.Length > 0)
                return valueByLevel[Mathf.Clamp(level, 1, valueByLevel.Length) - 1];

            return kind switch
            {
                ModifierKind.Multiply => Mathf.Pow(valuePerLevel, level),
                ModifierKind.Override => valuePerLevel,
                _ => valuePerLevel * level
            };
        }

        public void Apply(SkillEffectContext context, int level)
        {
            context.Stats.RemoveModifiers(statId, this);
            context.Stats.AddModifier(statId, kind, ValueAt(level), this);
        }

        public void Remove(SkillEffectContext context) => context.Stats.RemoveModifiers(statId, this);

        public string Describe(SkillTreeDefinition tree, int level)
        {
            tree.TryGetStat(statId, out StatDefinition stat);
            string name = stat?.DisplayName ?? statId;
            float value = ValueAt(Mathf.Max(level, 1));

            return kind switch
            {
                ModifierKind.Flat => $"{Signed(value, stat?.format == StatFormat.Percent)} {name}",
                ModifierKind.PercentAdd => $"{Signed(value, true)} {name}",
                ModifierKind.Multiply => $"x{value:0.##} {name}",
                _ => $"{name} = {Format(value, stat?.format == StatFormat.Percent)}"
            };
        }

        public IEnumerable<string> Validate(SkillTreeDefinition tree)
        {
            if (string.IsNullOrEmpty(statId)) yield return "Modify Stat has no stat selected.";
            else if (!tree.TryGetStat(statId, out _)) yield return $"Modify Stat uses unknown stat '{statId}'.";
        }

        static string Signed(float value, bool percent) => (value >= 0f ? "+" : "") + Format(value, percent);

        static string Format(float value, bool percent) => percent ? $"{value * 100f:0.##}%" : $"{value:0.##}";
    }
}
