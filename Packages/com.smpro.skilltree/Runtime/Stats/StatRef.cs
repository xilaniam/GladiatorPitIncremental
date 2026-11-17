using System;
using UnityEngine;

namespace SMPro.SkillTree
{
    [Serializable]
    public class StatRef
    {
        [SerializeField] SkillTreeDefinition tree;
        [SerializeField] string statId;

        [NonSerialized] Stat cached;
        [NonSerialized] int cachedGeneration = -1;

        public StatRef() { }

        public StatRef(SkillTreeDefinition tree, string statId)
        {
            this.tree = tree;
            this.statId = statId;
        }

        public SkillTreeDefinition Tree => tree;
        public string StatId => statId;
        public bool IsValid => tree != null && tree.TryGetStat(statId, out _);

        public Stat Stat
        {
            get
            {
                if (cached == null || cachedGeneration != StatRegistry.Generation)
                {
                    if (tree == null) throw new InvalidOperationException($"StatRef '{statId}' has no tree assigned.");
                    cached = tree.Stats.Get(statId);
                    cachedGeneration = StatRegistry.Generation;
                }
                return cached;
            }
        }

        public float Value => Stat.Value;
        public int IntValue => Stat.IntValue;

        public void AddModifier(ModifierKind kind, float value, object source) =>
            tree.Stats.AddModifier(statId, kind, value, source);

        public void AddTemporary(ModifierKind kind, float value, float duration, object source) =>
            tree.Stats.AddTemporary(statId, kind, value, duration, source);

        public void RemoveModifiers(object source) => tree.Stats.RemoveModifiers(statId, source);
    }
}
