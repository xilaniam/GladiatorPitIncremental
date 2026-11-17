using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree
{
    public class Stat
    {
        readonly List<StatModifier> modifiers = new();

        public StatDefinition Definition { get; }
        public string Id => Definition.id;
        public float Value { get; private set; }
        public int IntValue => Mathf.RoundToInt(Value);
        public IReadOnlyList<StatModifier> Modifiers => modifiers;

        public event Action<Stat, float, float> Changed;

        public Stat(StatDefinition definition)
        {
            Definition = definition;
            Value = Calculate();
        }

        internal void Add(StatModifier modifier)
        {
            modifiers.Add(modifier);
            Refresh();
        }

        internal bool RemoveFrom(object source)
        {
            bool removed = false;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (!Equals(modifiers[i].Source, source)) continue;
                modifiers.RemoveAt(i);
                removed = true;
            }
            if (removed) Refresh();
            return removed;
        }

        internal bool RemoveExpired(float now)
        {
            bool removed = false;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].ExpiresAt > now) continue;
                modifiers.RemoveAt(i);
                removed = true;
            }
            if (removed) Refresh();
            return removed;
        }

        internal bool HasTemporary => modifiers.Exists(m => m.IsTemporary);

        internal void Clear() => RemoveWhere(_ => true);

        bool RemoveWhere(Predicate<StatModifier> match)
        {
            if (modifiers.RemoveAll(match) == 0) return false;
            Refresh();
            return true;
        }

        void Refresh()
        {
            float old = Value;
            Value = Calculate();
            if (!Mathf.Approximately(old, Value)) Changed?.Invoke(this, old, Value);
        }

        float Calculate()
        {
            float flat = 0f, percent = 0f, multiply = 1f;
            bool hasOverride = false;
            float overrideValue = 0f;

            foreach (StatModifier modifier in modifiers)
            {
                switch (modifier.Kind)
                {
                    case ModifierKind.Flat: flat += modifier.Value; break;
                    case ModifierKind.PercentAdd: percent += modifier.Value; break;
                    case ModifierKind.Multiply: multiply *= modifier.Value; break;
                    case ModifierKind.Override:
                        hasOverride = true;
                        overrideValue = modifier.Value;
                        break;
                }
            }

            float result = hasOverride ? overrideValue : (Definition.baseValue + flat) * (1f + percent) * multiply;
            result = Mathf.Clamp(result, Definition.min, Definition.max);
            return Definition.isInteger ? (float)Math.Round(result, MidpointRounding.AwayFromZero) : result;
        }
    }
}
