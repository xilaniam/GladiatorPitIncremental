using System.Collections;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public class StateTests : InspectorTestBase
    {
        StateTestComponent component;
        VisualElement root;
        InspectorRefresher refresher;

        void Build()
        {
            component = NewComponent<StateTestComponent>();
            root = Inspect(component);
            refresher = InspectorBuilder.RefresherOf(root);
        }

        static bool Shown(VisualElement element) => element.style.display.value != DisplayStyle.None;

        bool Shown(string name) => Shown(root.Q(name));

        bool Enabled(string name) => root.Q(name).enabledSelf;

        void Set(System.Action<StateTestComponent> change)
        {
            change(component);
            refresher.Refresh();
        }

        [Test]
        public void ShowIf_And_HideIf_FollowABool()
        {
            Build();
            Assert.IsFalse(Shown("PropertyField:shownWhenOn"));
            Assert.IsTrue(Shown("PropertyField:hiddenWhenOn"));
            Assert.IsTrue(Shown("PropertyField:shownWhenOff"));

            Set(c => c.toggle = true);

            Assert.IsTrue(Shown("PropertyField:shownWhenOn"));
            Assert.IsFalse(Shown("PropertyField:hiddenWhenOn"));
            Assert.IsFalse(Shown("PropertyField:shownWhenOff"));
        }

        [Test]
        public void ShowIf_MatchesAnyOfSeveralEnumValues()
        {
            Build();
            Assert.IsFalse(Shown("PropertyField:advanced"));

            Set(c => c.mode = StateTestComponent.Mode.Expert);
            Assert.IsTrue(Shown("PropertyField:advanced"));

            Set(c => c.mode = StateTestComponent.Mode.Simple);
            Assert.IsFalse(Shown("PropertyField:advanced"));
        }

        [Test]
        public void EnableIf_DisableIf_AndReadOnly_SetTheEnabledState()
        {
            Build();
            Assert.IsFalse(Enabled("PropertyField:enabledWhenOn"));
            Assert.IsTrue(Enabled("PropertyField:disabledWhenOn"));
            Assert.IsFalse(Enabled("PropertyField:locked"));

            Set(c => c.toggle = true);

            Assert.IsTrue(Enabled("PropertyField:enabledWhenOn"));
            Assert.IsFalse(Enabled("PropertyField:disabledWhenOn"));
            Assert.IsFalse(Enabled("PropertyField:locked"));
        }

        [Test]
        public void SeveralConditions_MustAllAllow()
        {
            Build();
            Set(c => c.toggle = true);
            Assert.IsTrue(Shown("PropertyField:bothConditions"));

            Set(c => c.mode = StateTestComponent.Mode.Expert);
            Assert.IsFalse(Shown("PropertyField:bothConditions"));
        }

        [Test]
        public void PlayModeAttributes_FollowTheEditorMode()
        {
            Build();

            Assert.IsFalse(Shown("PropertyField:playOnly"));
            Assert.IsTrue(Shown("PropertyField:editOnly"));
        }

        [Test]
        public void Group_Hides_WhenAllItsMembersAreHidden()
        {
            Build();
            Assert.IsFalse(Shown("sm-group:Box"));

            Set(c => c.toggle = true);
            Assert.IsTrue(Shown("sm-group:Box"));
        }

        [Test]
        public void RowCell_Hides_ButTheRowStays()
        {
            Build();
            VisualElement row = root.Q("sm-group:Row");
            VisualElement hiddenCell = root.Q("PropertyField:rowA").parent;

            Assert.IsTrue(Shown(row));
            Assert.IsFalse(Shown(hiddenCell));
            Assert.IsTrue(Shown(root.Q("PropertyField:rowB").parent));
        }

        [Test]
        public void InfoBox_VisibleIf_FollowsAPropertyCondition()
        {
            Build();
            HelpBox box = root.Q("sm-property:counted").Q<HelpBox>();
            Assert.IsFalse(Shown(box));

            Set(c => c.count = 20);
            Assert.IsTrue(Shown(box));
        }

        [Test]
        public void Condition_HidesTheWholeDecoratedField()
        {
            Build();

            Assert.IsFalse(Shown("sm-property:decorated"));
        }

        [Test]
        public void NestedClass_ConditionsReadTheNestedObject_ThenTheOwner()
        {
            Build();
            Assert.IsFalse(Shown("PropertyField:nested.inner"));
            Assert.IsFalse(Shown("PropertyField:nested.usesOuter"));

            Set(c => c.nested.on = true);
            Assert.IsTrue(Shown("PropertyField:nested.inner"));

            Set(c => c.toggle = true);
            Assert.IsTrue(Shown("PropertyField:nested.usesOuter"));
        }

        [Test]
        public void Polling_IsOnlyUsed_WhenAConditionReadsANonSerializedMember()
        {
            Build();
            Assert.IsTrue(refresher.Polls);

            VisualElement fieldsOnly = Inspect(NewComponent<FieldConditionTestComponent>());
            Assert.IsFalse(InspectorBuilder.RefresherOf(fieldsOnly).Polls);
        }

        [Test]
        public void UnknownMember_ShowsAnError_AndKeepsTheFieldVisible()
        {
            LogAssert.Expect(LogType.Warning, "SMInspectorPro: FieldConditionTestComponent.broken: ShowIf: No member 'missingMember' on FieldConditionTestComponent");

            VisualElement fieldsOnly = Inspect(NewComponent<FieldConditionTestComponent>());
            VisualElement broken = fieldsOnly.Q("sm-property:broken");

            Assert.AreEqual("ShowIf: No member 'missingMember' on FieldConditionTestComponent", broken.Q<HelpBox>().text);
            Assert.IsTrue(Shown(broken.Q("PropertyField:broken")));
        }

        [Test]
        public void Refresh_DoesNotAllocate()
        {
            Build();
            refresher.Refresh();

            Assert.That(() => refresher.Refresh(), UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator LiveInspector_UpdatesWhenAValueChanges()
        {
            var target = NewComponent<FieldConditionTestComponent>();
            var window = ScriptableObject.CreateInstance<InspectorTestWindow>();
            window.Show();
            try
            {
                var inspector = new InspectorElement(target);
                window.rootVisualElement.Add(inspector);
                yield return WaitUntil(() => inspector.Q("PropertyField:shownWhenOn") != null);
                VisualElement field = inspector.Q("PropertyField:shownWhenOn");
                Assert.IsFalse(Shown(field));

                var serializedObject = new SerializedObject(target);
                serializedObject.FindProperty(nameof(FieldConditionTestComponent.toggle)).boolValue = true;
                serializedObject.ApplyModifiedProperties();

                yield return WaitUntil(() => Shown(field));
                Assert.IsTrue(Shown(field));
            }
            finally
            {
                window.Close();
            }
        }

        static IEnumerator WaitUntil(System.Func<bool> condition)
        {
            double deadline = EditorApplication.timeSinceStartup + 5.0;
            while (!condition() && EditorApplication.timeSinceStartup < deadline) yield return null;
        }
    }
}
