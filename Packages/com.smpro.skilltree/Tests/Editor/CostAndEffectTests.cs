using NUnit.Framework;

namespace SMPro.SkillTree.Tests
{
    public class CostAndEffectTests
    {
        [Test]
        public void FlatCost_IsConstant()
        {
            var cost = new FlatCost { amount = 3 };

            Assert.AreEqual(3, cost.GetCost(0));
            Assert.AreEqual(3, cost.GetCost(5));
        }

        [Test]
        public void LinearCost_GrowsByStep()
        {
            var cost = new LinearCost { baseCost = 10, step = 5 };

            Assert.AreEqual(10, cost.GetCost(0));
            Assert.AreEqual(20, cost.GetCost(2));
        }

        [Test]
        public void ExponentialCost_RoundsUp()
        {
            var cost = new ExponentialCost { baseCost = 2, growth = 1.5f };

            Assert.AreEqual(2, cost.GetCost(0));
            Assert.AreEqual(3, cost.GetCost(1));
            Assert.AreEqual(5, cost.GetCost(2));
        }

        [Test]
        public void ExponentialCost_IgnoresFloatRepresentationError()
        {
            var cost = new ExponentialCost { baseCost = 15000, growth = 2.2f };

            Assert.AreEqual(33000, cost.GetCost(1));
            Assert.AreEqual(72600, cost.GetCost(2));
        }

        [Test]
        public void ExponentialCost_SaturatesInsteadOfOverflowing()
        {
            var cost = new ExponentialCost { baseCost = 1000, growth = 10f };

            Assert.AreEqual(long.MaxValue, cost.GetCost(40));
        }

        [Test]
        public void TableCost_ClampsToLastEntry()
        {
            var cost = new TableCost { costs = new long[] { 1, 4, 9 } };

            Assert.AreEqual(4, cost.GetCost(1));
            Assert.AreEqual(9, cost.GetCost(7));
        }

        [Test]
        public void ModifyStat_FlatScalesLinearly()
        {
            var effect = TestTrees.Modify("coins", ModifierKind.Flat, 0.2f);

            Assert.AreEqual(0.6f, effect.ValueAt(3), 0.0001f);
        }

        [Test]
        public void ModifyStat_MultiplyCompounds()
        {
            var effect = TestTrees.Modify("damage", ModifierKind.Multiply, 1.2f);

            Assert.AreEqual(1.728f, effect.ValueAt(3), 0.0001f);
        }

        [Test]
        public void ModifyStat_TableOverridesPerLevelValue()
        {
            var effect = TestTrees.Modify("damage", ModifierKind.Flat, 1f);
            effect.valueByLevel = new[] { 2f, 5f };

            Assert.AreEqual(5f, effect.ValueAt(2));
            Assert.AreEqual(5f, effect.ValueAt(4));
        }

        [Test]
        public void ModifyStat_DescribesWithStatName()
        {
            var stat = TestTrees.Stat("coins.multiplier");
            stat.displayName = "Coin Value";
            SkillTreeDefinition tree = TestTrees.Create(new[] { stat });
            var effect = TestTrees.Modify("coins.multiplier", ModifierKind.PercentAdd, 0.2f);

            Assert.AreEqual("+40% Coin Value", effect.Describe(tree, 2));
        }

        [Test]
        public void ModifyStat_ReportsUnknownStat()
        {
            SkillTreeDefinition tree = TestTrees.Create(new[] { TestTrees.Stat("damage") });
            var effect = TestTrees.Modify("speed", ModifierKind.Flat, 1f);

            CollectionAssert.IsNotEmpty(effect.Validate(tree));
        }
    }
}
