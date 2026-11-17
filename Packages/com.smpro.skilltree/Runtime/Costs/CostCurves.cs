using System;
using UnityEngine;

namespace SMPro.SkillTree
{
    [Serializable]
    public abstract class CostCurve
    {
        public abstract long GetCost(int currentLevel);
    }

    [Serializable]
    public class FlatCost : CostCurve
    {
        public long amount = 1;

        public override long GetCost(int currentLevel) => amount;
    }

    [Serializable]
    public class LinearCost : CostCurve
    {
        public long baseCost = 1;
        public long step = 1;

        public override long GetCost(int currentLevel) => baseCost + step * currentLevel;
    }

    [Serializable]
    public class ExponentialCost : CostCurve
    {
        public long baseCost = 1;
        [Min(1f)] public float growth = 1.5f;

        public override long GetCost(int currentLevel) => (long)Math.Ceiling(baseCost * Math.Pow(growth, currentLevel));
    }

    [Serializable]
    public class TableCost : CostCurve
    {
        public long[] costs = Array.Empty<long>();

        public override long GetCost(int currentLevel) =>
            costs == null || costs.Length == 0 ? 0 : costs[Mathf.Clamp(currentLevel, 0, costs.Length - 1)];
    }
}
