using System;
using System.Collections.Generic;

namespace SMPro.SkillTree
{
    public class StatCollection
    {
        readonly Dictionary<string, Stat> stats = new();
        readonly HashSet<Stat> withTemporary = new();
        readonly List<Stat> tickBuffer = new();
        float time;

        public IEnumerable<Stat> All => stats.Values;

        public event Action<Stat, float, float> StatChanged;

        public StatCollection(IEnumerable<StatDefinition> definitions)
        {
            foreach (StatDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.id) || stats.ContainsKey(definition.id)) continue;
                var stat = new Stat(definition);
                stat.Changed += (s, oldValue, newValue) => StatChanged?.Invoke(s, oldValue, newValue);
                stats.Add(definition.id, stat);
            }
        }

        public bool TryGet(string statId, out Stat stat)
        {
            stat = null;
            return !string.IsNullOrEmpty(statId) && stats.TryGetValue(statId, out stat);
        }

        public Stat Get(string statId)
        {
            if (TryGet(statId, out Stat stat)) return stat;
            throw new ArgumentException($"Unknown stat '{statId}'.");
        }

        public float GetValue(string statId) => Get(statId).Value;

        public void AddModifier(string statId, ModifierKind kind, float value, object source)
        {
            Get(statId).Add(new StatModifier(kind, value, source));
        }

        public void AddTemporary(string statId, ModifierKind kind, float value, float duration, object source)
        {
            Stat stat = Get(statId);
            stat.Add(new StatModifier(kind, value, source, time + duration));
            withTemporary.Add(stat);
        }

        public void RemoveModifiers(string statId, object source)
        {
            if (TryGet(statId, out Stat stat)) stat.RemoveFrom(source);
        }

        public void RemoveModifiers(object source)
        {
            foreach (Stat stat in stats.Values) stat.RemoveFrom(source);
        }

        public void ClearModifiers()
        {
            foreach (Stat stat in stats.Values) stat.Clear();
            withTemporary.Clear();
        }

        public void Tick(float now)
        {
            time = now;
            if (withTemporary.Count == 0) return;

            tickBuffer.Clear();
            tickBuffer.AddRange(withTemporary);
            foreach (Stat stat in tickBuffer) stat.RemoveExpired(now);
            withTemporary.RemoveWhere(s => !s.HasTemporary);
        }
    }
}
