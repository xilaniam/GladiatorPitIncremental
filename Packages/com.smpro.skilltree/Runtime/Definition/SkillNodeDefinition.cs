using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree
{
    public enum RevealRule
    {
        Always,
        WhenParentStarted,
        WhenRequirementsMet,
        MysteryUntilAvailable
    }

    [Serializable]
    public class NodeRequirement
    {
        [NodeId] public string nodeId;
        [Min(1)] public int level = 1;
    }

    [Serializable]
    public class SkillNodeDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string flavourText;
        public Sprite icon;
        public Vector2 position;
        [Min(1)] public int maxLevel = 1;
        public string currencyId = CurrencyIds.Default;
        [SerializeReference, TypePicker] public CostCurve cost = new FlatCost();
        public List<NodeRequirement> requirements = new();
        [SerializeReference, TypePicker] public List<ISkillCondition> conditions = new();
        [SerializeReference, TypePicker] public List<ISkillEffect> effects = new();
        public RevealRule reveal = RevealRule.WhenParentStarted;
        public string[] tags = Array.Empty<string>();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;

        public long GetCost(int currentLevel) => cost?.GetCost(currentLevel) ?? 0;

        public long GetTotalCost(int level)
        {
            long total = 0;
            for (int i = 0; i < level; i++)
            {
                long cost = GetCost(i);
                if (total > long.MaxValue - cost) return long.MaxValue;
                total += cost;
            }
            return total;
        }

        public bool HasTag(string tag) => tags != null && Array.IndexOf(tags, tag) >= 0;
    }
}
