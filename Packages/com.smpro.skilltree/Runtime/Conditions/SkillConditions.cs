using System;
using System.Collections.Generic;

namespace SMPro.SkillTree
{
    public interface ISkillCondition
    {
        bool IsMet(SkillTreeController tree);
        string Describe(SkillTreeDefinition tree);
        IEnumerable<string> Validate(SkillTreeDefinition tree) => Array.Empty<string>();
    }

    [Serializable]
    public class TotalPointsSpentCondition : ISkillCondition
    {
        public int points = 1;

        public bool IsMet(SkillTreeController tree) => tree.TotalLevels >= points;

        public string Describe(SkillTreeDefinition tree) => $"Requires {points} points spent";
    }

    [Serializable]
    public class TagOwnedCondition : ISkillCondition
    {
        public string tag;

        public bool IsMet(SkillTreeController tree) => tree.HasTag(tag);

        public string Describe(SkillTreeDefinition tree) => $"Requires {tag}";

        public IEnumerable<string> Validate(SkillTreeDefinition tree)
        {
            if (string.IsNullOrEmpty(tag)) yield return "Tag Owned has no tag.";
        }
    }

    [Serializable]
    public class StatAtLeastCondition : ISkillCondition
    {
        [StatId] public string statId;
        public float value;

        public bool IsMet(SkillTreeController tree) => tree.Stats.GetValue(statId) >= value;

        public string Describe(SkillTreeDefinition tree) => $"Requires {tree.StatName(statId)} {value:0.##} or more";

        public IEnumerable<string> Validate(SkillTreeDefinition tree)
        {
            if (!tree.TryGetStat(statId, out _)) yield return $"Stat At Least uses unknown stat '{statId}'.";
        }
    }
}
