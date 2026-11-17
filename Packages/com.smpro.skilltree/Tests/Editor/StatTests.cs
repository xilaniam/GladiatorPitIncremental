using NUnit.Framework;

namespace SMPro.SkillTree.Tests
{
    public class StatTests
    {
        static StatCollection Collection(StatDefinition definition) => new(new[] { definition });

        [Test]
        public void Value_IsBase_WithoutModifiers()
        {
            StatCollection stats = Collection(TestTrees.Stat("damage", 10f));

            Assert.AreEqual(10f, stats.GetValue("damage"));
        }

        [Test]
        public void Value_CombinesFlatPercentAndMultiply()
        {
            StatCollection stats = Collection(TestTrees.Stat("damage", 10f));
            object source = new object();

            stats.AddModifier("damage", ModifierKind.Flat, 5f, source);
            stats.AddModifier("damage", ModifierKind.PercentAdd, 0.2f, source);
            stats.AddModifier("damage", ModifierKind.PercentAdd, 0.3f, source);
            stats.AddModifier("damage", ModifierKind.Multiply, 2f, source);

            Assert.AreEqual(45f, stats.GetValue("damage"), 0.0001f);
        }

        [Test]
        public void Override_WinsOverOtherModifiers()
        {
            StatCollection stats = Collection(TestTrees.Stat("damage", 10f));

            stats.AddModifier("damage", ModifierKind.Flat, 5f, this);
            stats.AddModifier("damage", ModifierKind.Override, 3f, this);

            Assert.AreEqual(3f, stats.GetValue("damage"));
        }

        [Test]
        public void Value_IsClampedAndRounded()
        {
            var definition = TestTrees.Stat("count", 1f);
            definition.max = 4f;
            definition.isInteger = true;
            StatCollection stats = Collection(definition);

            stats.AddModifier("count", ModifierKind.Flat, 1.5f, this);
            Assert.AreEqual(3f, stats.GetValue("count"));

            stats.AddModifier("count", ModifierKind.Flat, 10f, this);
            Assert.AreEqual(4f, stats.GetValue("count"));
        }

        [Test]
        public void RemoveModifiers_RemovesOnlyThatSource()
        {
            StatCollection stats = Collection(TestTrees.Stat("damage", 10f));
            object a = new object(), b = new object();

            stats.AddModifier("damage", ModifierKind.Flat, 1f, a);
            stats.AddModifier("damage", ModifierKind.Flat, 2f, b);
            stats.RemoveModifiers(a);

            Assert.AreEqual(12f, stats.GetValue("damage"));
        }

        [Test]
        public void TemporaryModifier_ExpiresOnTick()
        {
            StatCollection stats = Collection(TestTrees.Stat("interval", 1f));
            stats.Tick(10f);

            stats.AddTemporary("interval", ModifierKind.Multiply, 0.5f, 5f, this);
            stats.Tick(14f);
            Assert.AreEqual(0.5f, stats.GetValue("interval"));

            stats.Tick(15f);
            Assert.AreEqual(1f, stats.GetValue("interval"));
        }

        [Test]
        public void StatChanged_ReportsOldAndNewValue()
        {
            StatCollection stats = Collection(TestTrees.Stat("damage", 10f));
            float oldValue = 0f, newValue = 0f;
            stats.StatChanged += (_, o, n) => { oldValue = o; newValue = n; };

            stats.AddModifier("damage", ModifierKind.Flat, 5f, this);

            Assert.AreEqual(10f, oldValue);
            Assert.AreEqual(15f, newValue);
        }

        [Test]
        public void UnknownStat_Throws()
        {
            StatCollection stats = Collection(TestTrees.Stat("damage"));

            Assert.Throws<System.ArgumentException>(() => stats.GetValue("speed"));
        }
    }
}
