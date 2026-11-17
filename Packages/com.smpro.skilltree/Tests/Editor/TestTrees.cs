using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SMPro.SkillTree.Tests
{
    internal static class TestTrees
    {
        public static SkillTreeDefinition Create(IEnumerable<StatDefinition> stats, params SkillNodeDefinition[] nodes)
        {
            var tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
            tree.treeId = "test";
            tree.stats = stats.ToList();
            tree.nodes = nodes.ToList();
            return tree;
        }

        public static StatDefinition Stat(string id, float baseValue = 0f) => new() { id = id, baseValue = baseValue };

        public static SkillNodeDefinition Node(string id, int maxLevel = 1, long cost = 0, params ISkillEffect[] effects) => new()
        {
            id = id,
            maxLevel = maxLevel,
            cost = new FlatCost { amount = cost },
            effects = effects.ToList(),
            reveal = RevealRule.Always
        };

        public static SkillNodeDefinition Requires(this SkillNodeDefinition node, string parentId, int level = 1)
        {
            node.requirements.Add(new NodeRequirement { nodeId = parentId, level = level });
            return node;
        }

        public static SkillNodeDefinition Reveal(this SkillNodeDefinition node, RevealRule rule)
        {
            node.reveal = rule;
            return node;
        }

        public static ModifyStatEffect Modify(string statId, ModifierKind kind, float valuePerLevel) =>
            new() { statId = statId, kind = kind, valuePerLevel = valuePerLevel };
    }
}
