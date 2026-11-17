using System.Collections.Generic;

namespace SMPro.SkillTree
{
    internal class SourceSet
    {
        readonly Dictionary<string, HashSet<object>> sources = new();

        public bool Add(string key, object source)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (!sources.TryGetValue(key, out HashSet<object> set)) sources[key] = set = new HashSet<object>();
            return set.Add(source) && set.Count == 1;
        }

        public bool Remove(string key, object source)
        {
            if (string.IsNullOrEmpty(key) || !sources.TryGetValue(key, out HashSet<object> set)) return false;
            return set.Remove(source) && set.Count == 0;
        }

        public bool Contains(string key) =>
            !string.IsNullOrEmpty(key) && sources.TryGetValue(key, out HashSet<object> set) && set.Count > 0;

        public void Clear() => sources.Clear();
    }
}
