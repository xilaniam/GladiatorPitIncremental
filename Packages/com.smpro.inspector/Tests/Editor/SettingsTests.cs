using System.Collections.Generic;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public class SettingsTests : InspectorTestBase
    {
        SMInspectorSettings settings;
        bool useFallback;
        int pollInterval;
        List<string> skipped;

        [SetUp]
        public void Remember()
        {
            settings = SMInspectorSettings.instance;
            useFallback = settings.useFallbackInspector;
            pollInterval = settings.pollInterval;
            skipped = new List<string>(settings.skippedNamespaces);
        }

        [TearDown]
        public void Restore()
        {
            settings.useFallbackInspector = useFallback;
            settings.pollInterval = pollInterval;
            settings.skippedNamespaces = skipped;
        }

        [Test]
        public void TurningTheFallbackOff_GivesUnitysDefaultInspector()
        {
            settings.useFallbackInspector = false;

            VisualElement root = Inspect(NewComponent<AttributedTestComponent>());

            Assert.IsNull(root.Q("SMInspectorPro"));
            Assert.IsNotNull(root.Q<PropertyField>("PropertyField:marked"));
        }

        [Test]
        public void SkippedNamespaces_KeepUnitysDefaultInspector()
        {
            settings.skippedNamespaces = new List<string> { "SMPro.Inspector" };

            Assert.IsNull(Inspect(NewComponent<AttributedTestComponent>()).Q("SMInspectorPro"));
        }

        [Test]
        public void SkippedNamespaces_MatchWholeNamespaceParts()
        {
            settings.skippedNamespaces = new List<string> { "SMPro.Insp", " " };

            Assert.IsFalse(settings.IsSkipped(typeof(AttributedTestComponent)));
            Assert.IsNotNull(Inspect(NewComponent<AttributedTestComponent>()).Q("SMInspectorPro"));
        }

        [Test]
        public void EditorsDerivedFromSMInspectorEditor_IgnoreTheFallbackSetting()
        {
            settings.useFallbackInspector = false;

            Assert.IsNotNull(Inspect(NewComponent<DerivedEditorTestComponent>()).Q("SMInspectorPro"));
        }

        [Test]
        public void PollInterval_HasAMinimum()
        {
            settings.pollInterval = 5;
            Assert.AreEqual(SMInspectorSettings.MinimumPollInterval, settings.PollInterval);

            settings.pollInterval = 250;
            Assert.AreEqual(250, settings.PollInterval);
        }

        [Test]
        public void SettingsPage_HasAFieldForEachSetting()
        {
            var root = new VisualElement();

            SMInspectorSettingsProvider.Build(root);

            foreach (string name in new[] { "useFallbackInspector", "skippedNamespaces", "pollInterval", "validateBeforeBuild", "failBuildOnErrors" })
                Assert.IsNotNull(root.Q<PropertyField>("PropertyField:" + name), name);
            Assert.IsNotNull(root.Q<Button>("sm-open-validator"));
            Assert.AreEqual("Project/SMPro/Inspector", SMInspectorSettingsProvider.Create().settingsPath);
        }
    }
}
