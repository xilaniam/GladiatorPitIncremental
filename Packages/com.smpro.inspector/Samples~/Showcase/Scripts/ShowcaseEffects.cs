using System;
using SMPro.Inspector;

namespace SMPro.Inspector.Samples
{
    public interface IShowcaseEffect
    {
        string Describe();
    }

    [Serializable]
    public class DamageEffect : IShowcaseEffect
    {
        [MinValue(0)] public int amount = 10;
        public bool critical;
        [ShowIf(nameof(critical)), Suffix("x")] public float multiplier = 2f;

        public string Describe() => critical ? $"{amount} damage, x{multiplier} on crits" : $"{amount} damage";
    }

    [Serializable]
    public class HealEffect : IShowcaseEffect
    {
        [ProgressBar(0, 100)] public int percent = 25;

        public string Describe() => $"Heals {percent}%";
    }

    [Serializable]
    public class ShieldEffect : IShowcaseEffect
    {
        [MinValue(0), Suffix("s")] public float duration = 3f;

        public string Describe() => $"Shield for {duration}s";
    }
}
