using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SMPro.SkillTree.Tests
{
    public class ControllerTests
    {
        GameObject host;
        SimpleWallet wallet;

        [SetUp]
        public void SetUp()
        {
            StatRegistry.Clear();
            SkillFeatures.Clear();
            host = new GameObject("SkillTreeTest");
            wallet = host.AddComponent<SimpleWallet>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            StatRegistry.Clear();
            SkillFeatures.Clear();
        }

        SkillTreeController Controller(SkillTreeDefinition tree, long coins = 100)
        {
            wallet.Add(CurrencyIds.Default, coins);
            var controller = host.AddComponent<SkillTreeController>();
            controller.Initialize(tree, wallet);
            return controller;
        }

        [Test]
        public void TryPurchase_SpendsCostAndRaisesLevel()
        {
            SkillTreeController controller = Controller(TestTrees.Create(new StatDefinition[0], TestTrees.Node("a", 3, 10)));

            Assert.IsTrue(controller.TryPurchase("a"));
            Assert.AreEqual(1, controller.GetLevel("a"));
            Assert.AreEqual(90, wallet.GetBalance(CurrencyIds.Default));
        }

        [Test]
        public void CanPurchase_ReportsEachReason()
        {
            var locked = TestTrees.Node("child").Requires("root", 2);
            var gated = TestTrees.Node("gated");
            gated.conditions.Add(new TagOwnedCondition { tag = "crits" });
            SkillTreeDefinition tree = TestTrees.Create(new StatDefinition[0],
                TestTrees.Node("root", 2), TestTrees.Node("pricey", 1, 500), locked, gated);
            SkillTreeController controller = Controller(tree);

            Assert.AreEqual(PurchaseCheck.UnknownNode, controller.CanPurchase("missing"));
            Assert.AreEqual(PurchaseCheck.Funds, controller.CanPurchase("pricey"));
            Assert.AreEqual(PurchaseCheck.Requirements, controller.CanPurchase("child"));
            Assert.AreEqual(PurchaseCheck.Conditions, controller.CanPurchase("gated"));

            controller.TryPurchase("root");
            controller.TryPurchase("root");
            Assert.AreEqual(PurchaseCheck.Maxed, controller.CanPurchase("root"));
            Assert.AreEqual(PurchaseCheck.Ok, controller.CanPurchase("child"));
        }

        [Test]
        public void Purchase_AppliesStatModifiersPerLevel()
        {
            var node = TestTrees.Node("sharp", 3, 1, TestTrees.Modify("damage", ModifierKind.Multiply, 1.2f));
            SkillTreeDefinition tree = TestTrees.Create(new[] { TestTrees.Stat("damage", 10f) }, node);
            SkillTreeController controller = Controller(tree);

            controller.TryPurchase("sharp");
            controller.TryPurchase("sharp");

            Assert.AreEqual(14.4f, controller.Stats.GetValue("damage"), 0.0001f);
        }

        [Test]
        public void StatRef_ReadsLiveValue()
        {
            var node = TestTrees.Node("sharp", 1, 0, TestTrees.Modify("damage", ModifierKind.Flat, 5f));
            SkillTreeDefinition tree = TestTrees.Create(new[] { TestTrees.Stat("damage", 10f) }, node);
            var statRef = new StatRef(tree, "damage");
            SkillTreeController controller = Controller(tree);

            Assert.AreEqual(10f, statRef.Value);
            controller.TryPurchase("sharp");
            Assert.AreEqual(15f, statRef.Value);
        }

        [Test]
        public void GetState_FollowsRevealRules()
        {
            SkillTreeDefinition tree = TestTrees.Create(new StatDefinition[0],
                TestTrees.Node("root", 2),
                TestTrees.Node("always").Requires("root", 2),
                TestTrees.Node("parent").Requires("root", 2).Reveal(RevealRule.WhenParentStarted),
                TestTrees.Node("met").Requires("root", 2).Reveal(RevealRule.WhenRequirementsMet),
                TestTrees.Node("mystery").Requires("root", 2).Reveal(RevealRule.MysteryUntilAvailable));
            SkillTreeController controller = Controller(tree);

            Assert.AreEqual(NodeState.Available, controller.GetState("root"));
            Assert.AreEqual(NodeState.Locked, controller.GetState("always"));
            Assert.AreEqual(NodeState.Hidden, controller.GetState("parent"));
            Assert.AreEqual(NodeState.Hidden, controller.GetState("met"));
            Assert.AreEqual(NodeState.Mystery, controller.GetState("mystery"));

            controller.TryPurchase("root");
            Assert.AreEqual(NodeState.Locked, controller.GetState("parent"));
            Assert.AreEqual(NodeState.Hidden, controller.GetState("met"));
            Assert.AreEqual(NodeState.Mystery, controller.GetState("mystery"));

            controller.TryPurchase("root");
            Assert.AreEqual(NodeState.Maxed, controller.GetState("root"));
            Assert.AreEqual(NodeState.Available, controller.GetState("met"));
            Assert.AreEqual(NodeState.Available, controller.GetState("mystery"));
        }

        [Test]
        public void CaptureAndRestore_RebuildsLevelsAndStats()
        {
            var node = TestTrees.Node("sharp", 3, 1, TestTrees.Modify("damage", ModifierKind.Flat, 2f));
            SkillTreeDefinition tree = TestTrees.Create(new[] { TestTrees.Stat("damage", 10f) }, node);
            SkillTreeController controller = Controller(tree);
            controller.TryPurchase("sharp");
            controller.TryPurchase("sharp");

            string json = controller.CaptureState().ToJson();
            controller.ResetTree(0f);
            Assert.AreEqual(10f, controller.Stats.GetValue("damage"));

            controller.RestoreState(SkillTreeState.FromJson(json));
            Assert.AreEqual(2, controller.GetLevel("sharp"));
            Assert.AreEqual(14f, controller.Stats.GetValue("damage"));
        }

        [Test]
        public void RestoreState_IgnoresUnknownNodesAndClampsLevels()
        {
            SkillTreeDefinition tree = TestTrees.Create(new StatDefinition[0], TestTrees.Node("a", 2));
            SkillTreeController controller = Controller(tree);
            var state = new SkillTreeState { treeId = "test" };
            state.levels.Add(new SkillTreeState.NodeLevel { nodeId = "a", level = 9 });
            state.levels.Add(new SkillTreeState.NodeLevel { nodeId = "gone", level = 1 });

            LogAssert.Expect(LogType.Warning, new Regex("unknown node 'gone'"));
            controller.RestoreState(state);

            Assert.AreEqual(2, controller.GetLevel("a"));
        }

        [Test]
        public void ResetTree_RefundsAndClearsStats()
        {
            var node = TestTrees.Node("sharp", 2, 10, TestTrees.Modify("damage", ModifierKind.Flat, 1f));
            SkillTreeDefinition tree = TestTrees.Create(new[] { TestTrees.Stat("damage", 10f) }, node);
            SkillTreeController controller = Controller(tree);
            controller.TryPurchase("sharp");
            controller.TryPurchase("sharp");

            controller.ResetTree(0.5f);

            Assert.AreEqual(0, controller.GetLevel("sharp"));
            Assert.AreEqual(10f, controller.Stats.GetValue("damage"));
            Assert.AreEqual(90, wallet.GetBalance(CurrencyIds.Default));
        }

        [Test]
        public void TryRefund_IsBlockedWhileAChildIsOwned()
        {
            SkillTreeDefinition tree = TestTrees.Create(new StatDefinition[0],
                TestTrees.Node("root", 1, 10), TestTrees.Node("child", 1, 10).Requires("root"));
            SkillTreeController controller = Controller(tree);
            controller.TryPurchase("root");
            controller.TryPurchase("child");

            Assert.IsFalse(controller.TryRefund("root"));
            Assert.IsTrue(controller.TryRefund("child"));
            Assert.IsTrue(controller.TryRefund("root"));
            Assert.AreEqual(100, wallet.GetBalance(CurrencyIds.Default));
        }

        [Test]
        public void UnlockFeature_UnlocksAndLocksRegisteredFeature()
        {
            var feature = host.AddComponent<TestFeature>();
            SetFeatureId(feature, "frenzy");
            SkillFeatures.Register(feature);
            var node = TestTrees.Node("frenzy", 1, 0, new UnlockFeatureEffect { featureId = "frenzy" });
            SkillTreeController controller = Controller(TestTrees.Create(new StatDefinition[0], node));

            controller.TryPurchase("frenzy");
            Assert.IsTrue(feature.IsUnlocked);
            Assert.AreEqual(1, feature.unlockCount);

            controller.ResetTree();
            Assert.IsFalse(feature.IsUnlocked);
            Assert.AreEqual(1, feature.lockCount);
        }

        [Test]
        public void FeatureRegisteredLate_StartsUnlocked()
        {
            var node = TestTrees.Node("frenzy", 1, 0, new UnlockFeatureEffect { featureId = "frenzy" });
            SkillTreeController controller = Controller(TestTrees.Create(new StatDefinition[0], node));
            controller.TryPurchase("frenzy");

            var feature = host.AddComponent<TestFeature>();
            SetFeatureId(feature, "frenzy");
            SkillFeatures.Register(feature);

            Assert.IsTrue(feature.IsUnlocked);
        }

        [Test]
        public void GrantTag_SatisfiesTagCondition()
        {
            var keenEye = TestTrees.Node("keen", 1, 0, new CompositeEffect
            {
                effects = { new GrantTagEffect { tag = "crits" }, TestTrees.Modify("crit", ModifierKind.Flat, 0.08f) }
            });
            var deadly = TestTrees.Node("deadly");
            deadly.conditions.Add(new TagOwnedCondition { tag = "crits" });
            SkillTreeDefinition tree = TestTrees.Create(new[] { TestTrees.Stat("crit") }, keenEye, deadly);
            SkillTreeController controller = Controller(tree);

            Assert.AreEqual(PurchaseCheck.Conditions, controller.CanPurchase("deadly"));
            controller.TryPurchase("keen");

            Assert.AreEqual(PurchaseCheck.Ok, controller.CanPurchase("deadly"));
            Assert.AreEqual(0.08f, controller.Stats.GetValue("crit"), 0.0001f);
        }

        static void SetFeatureId(SkillFeature feature, string id)
        {
            typeof(SkillFeature)
                .GetField("featureId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(feature, id);
        }
    }
}
