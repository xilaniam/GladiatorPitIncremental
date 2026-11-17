using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SMPro.SkillTree.Tests
{
    public class ValidationTests
    {
        static bool HasError(SkillTreeDefinition tree, string fragment) =>
            SkillTreeValidation.Validate(tree).Any(i => i.Severity == IssueSeverity.Error && i.Message.Contains(fragment));

        [Test]
        public void ValidTree_HasNoErrors()
        {
            SkillTreeDefinition tree = TestTrees.Create(new[] { TestTrees.Stat("damage") },
                TestTrees.Node("a", 2, 1, TestTrees.Modify("damage", ModifierKind.Flat, 1f)),
                TestTrees.Node("b", 1, 1, TestTrees.Modify("damage", ModifierKind.Flat, 1f)).Requires("a", 2));

            Assert.IsFalse(SkillTreeValidation.Validate(tree).Any(i => i.Severity == IssueSeverity.Error));
        }

        [Test]
        public void ReportsDuplicateNodeIds()
        {
            SkillTreeDefinition tree = TestTrees.Create(new StatDefinition[0], TestTrees.Node("a"), TestTrees.Node("a"));

            Assert.IsTrue(HasError(tree, "used twice"));
        }

        [Test]
        public void ReportsUnknownAndUnreachableRequirements()
        {
            SkillTreeDefinition tree = TestTrees.Create(new StatDefinition[0],
                TestTrees.Node("a", 2),
                TestTrees.Node("b").Requires("missing"),
                TestTrees.Node("c").Requires("a", 3));

            Assert.IsTrue(HasError(tree, "unknown node 'missing'"));
            Assert.IsTrue(HasError(tree, "max level is 2"));
        }

        [Test]
        public void ReportsCycles()
        {
            SkillTreeDefinition tree = TestTrees.Create(new StatDefinition[0],
                TestTrees.Node("a").Requires("c"),
                TestTrees.Node("b").Requires("a"),
                TestTrees.Node("c").Requires("b"));

            Assert.IsTrue(HasError(tree, "cycle"));
        }

        [Test]
        public void ReportsEmptyEffectSlotsAndUnknownStats()
        {
            var node = TestTrees.Node("a", 1, 0, TestTrees.Modify("speed", ModifierKind.Flat, 1f));
            node.effects.Add(null);
            SkillTreeDefinition tree = TestTrees.Create(new[] { TestTrees.Stat("damage") }, node);

            Assert.IsTrue(HasError(tree, "unknown stat 'speed'"));
            Assert.IsTrue(HasError(tree, "effect slot is empty"));
        }

        [Test]
        public void WarnsAboutMissingIcon()
        {
            SkillTreeDefinition tree = TestTrees.Create(new StatDefinition[0], TestTrees.Node("a"));

            Assert.IsTrue(SkillTreeValidation.Validate(tree).Any(i => i.Severity == IssueSeverity.Warning && i.Message.Contains("icon")));
        }
    }
}
