using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree
{
    internal static class StatRegistry
    {
        static readonly Dictionary<SkillTreeDefinition, StatCollection> collections = new();

        public static int Generation { get; private set; }

        public static StatCollection For(SkillTreeDefinition tree)
        {
            if (!collections.TryGetValue(tree, out StatCollection collection))
            {
                collection = new StatCollection(tree.stats);
                collections.Add(tree, collection);
            }
            return collection;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Clear()
        {
            collections.Clear();
            Generation++;
        }
    }
}
