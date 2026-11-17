using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree
{
    [CreateAssetMenu(menuName = "SkillTree/Skill Tree Definition", fileName = "SkillTree")]
    public class SkillTreeDefinition : ScriptableObject
    {
        public string treeId;
        public List<StatDefinition> stats = new();
        public List<SkillNodeDefinition> nodes = new();

        Dictionary<string, SkillNodeDefinition> nodeLookup;
        Dictionary<string, StatDefinition> statLookup;

        public StatCollection Stats => StatRegistry.For(this);

        public bool TryGetNode(string nodeId, out SkillNodeDefinition node)
        {
            node = null;
            return !string.IsNullOrEmpty(nodeId) && NodeLookup.TryGetValue(nodeId, out node);
        }

        public bool TryGetStat(string statId, out StatDefinition stat)
        {
            stat = null;
            return !string.IsNullOrEmpty(statId) && StatLookup.TryGetValue(statId, out stat);
        }

        public string StatName(string statId) => TryGetStat(statId, out StatDefinition stat) ? stat.DisplayName : statId;

        public void InvalidateLookups()
        {
            nodeLookup = null;
            statLookup = null;
        }

        public static string NewId() => Guid.NewGuid().ToString("N");

        Dictionary<string, SkillNodeDefinition> NodeLookup => nodeLookup ??= BuildLookup(nodes, n => n.id);
        Dictionary<string, StatDefinition> StatLookup => statLookup ??= BuildLookup(stats, s => s.id);

        static Dictionary<string, T> BuildLookup<T>(List<T> items, Func<T, string> key) where T : class
        {
            var lookup = new Dictionary<string, T>();
            foreach (T item in items)
            {
                string id = item == null ? null : key(item);
                if (!string.IsNullOrEmpty(id)) lookup[id] = item;
            }
            return lookup;
        }

        void OnValidate()
        {
            InvalidateLookups();
            if (string.IsNullOrEmpty(treeId)) treeId = name;
            EnsureUniqueNodeIds();
        }

        void EnsureUniqueNodeIds()
        {
            var seen = new HashSet<string>();
            foreach (SkillNodeDefinition node in nodes)
            {
                if (node == null) continue;
                if (string.IsNullOrEmpty(node.id) || !seen.Add(node.id))
                {
                    node.id = NewId();
                    seen.Add(node.id);
                }
            }
        }
    }
}
