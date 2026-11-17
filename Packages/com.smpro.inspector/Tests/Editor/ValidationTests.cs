using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public class ValidationTests : InspectorTestBase
    {
        ValidationTestComponent component;
        VisualElement root;
        InspectorRefresher refresher;

        void Build()
        {
            component = NewComponent<ValidationTestComponent>();
            root = Inspect(component);
            refresher = InspectorBuilder.RefresherOf(root);
        }

        void Change(Action<ValidationTestComponent> change)
        {
            change(component);
            refresher.Refresh();
        }

        HelpBox[] Messages(string path) => root.Q("sm-validation:" + path).Query<HelpBox>().ToList().ToArray();

        string[] Texts(string path) => Messages(path).Select(box => box.text).ToArray();

        [Test]
        public void Required_ShowsAnError_UntilAReferenceIsSet()
        {
            Build();
            CollectionAssert.AreEqual(new[] { "Required Object is required" }, Texts("requiredObject"));
            Assert.AreEqual(HelpBoxMessageType.Error, Messages("requiredObject")[0].messageType);

            Change(c => c.requiredObject = c.gameObject);

            Assert.IsEmpty(Texts("requiredObject"));
        }

        [Test]
        public void Required_UsesItsMessage_ForEmptyStrings()
        {
            Build();

            Change(c => c.title = string.Empty);

            CollectionAssert.AreEqual(new[] { "Give it a name" }, Texts("title"));
        }

        [Test]
        public void ValidateInput_AcceptsBoolAndStringResults()
        {
            Build();

            Change(c =>
            {
                c.even = 3;
                c.nickname = "bad";
            });

            CollectionAssert.AreEqual(new[] { "Must be even" }, Texts("even"));
            CollectionAssert.AreEqual(new[] { "Name is bad" }, Texts("nickname"));

            Change(c =>
            {
                c.even = 4;
                c.nickname = "good";
            });

            Assert.IsEmpty(Texts("even"));
            Assert.IsEmpty(Texts("nickname"));
        }

        [Test]
        public void ValidateInput_UsesItsSeverity()
        {
            Build();

            Change(c => c.score = -1f);

            Assert.AreEqual(HelpBoxMessageType.Warning, Messages("score").Single().messageType);
        }

        [Test]
        public void Limits_ClampValuesInTheInspector()
        {
            Build();

            Change(c =>
            {
                c.atLeastZero = -5;
                c.atMostTen = 15f;
                c.atLeastLowest = 1;
                c.unitRange = new Vector2(-1f, 2f);
            });

            Assert.AreEqual(0, component.atLeastZero);
            Assert.AreEqual(10f, component.atMostTen);
            Assert.AreEqual(5, component.atLeastLowest);
            Assert.AreEqual(new Vector2(0f, 1f), component.unitRange);
            Assert.IsEmpty(Texts("atLeastZero"));
        }

        [Test]
        public void Limits_AreReported_WhenFixesAreOff()
        {
            Build();
            component.atLeastZero = -5;
            component.atMostTen = 15f;
            var report = new ValidationReport();

            refresher.Validation.Run(report);

            string[] messages = report.Issues.Select(issue => issue.Message).ToArray();
            CollectionAssert.Contains(messages, "At Least Zero must be at least 0");
            CollectionAssert.Contains(messages, "At Most Ten must be at most 10");
            Assert.AreEqual(-5, component.atLeastZero);
        }

        [Test]
        public void AssetsOnly_And_SceneObjectsOnly_CheckWhereTheObjectLives()
        {
            Build();
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.smpro.inspector/package.json");

            Change(c =>
            {
                c.assetOnly = c.gameObject;
                c.sceneOnly = asset;
            });

            CollectionAssert.AreEqual(new[] { "Asset Only must be an asset, not a scene object" }, Texts("assetOnly"));
            CollectionAssert.AreEqual(new[] { "Scene Only must be a scene object, not an asset" }, Texts("sceneOnly"));

            Change(c =>
            {
                c.assetOnly = asset;
                c.sceneOnly = c.gameObject;
            });

            Assert.IsEmpty(Texts("assetOnly"));
            Assert.IsEmpty(Texts("sceneOnly"));
        }

        [Test]
        public void ChildrenOnly_AcceptsOnlyThisHierarchy()
        {
            Build();
            Transform outsider = NewComponent<PlainTestComponent>().transform;
            var inner = new GameObject("Inner") { hideFlags = HideFlags.HideAndDontSave };
            inner.transform.SetParent(component.transform);

            Change(c => c.child = outsider);
            CollectionAssert.AreEqual(new[] { "Child must be this object or one of its children" }, Texts("child"));

            Change(c => c.child = inner.transform);
            Assert.IsEmpty(Texts("child"));
        }

        [Test]
        public void SelfValidator_MessagesGoToTheirFieldOrTheTop()
        {
            Build();

            Change(c => c.selfChecked = 101);
            CollectionAssert.AreEqual(new[] { "Too big" }, Texts("selfChecked"));

            Change(c => c.selfChecked = 42);
            Assert.IsEmpty(Texts("selfChecked"));
            CollectionAssert.AreEqual(new[] { "Suspicious" }, Texts("root"));
        }

        [Test]
        public void NestedSelfValidator_ReportsUnderItsField()
        {
            Build();

            Change(c => c.nested.value = -1);

            CollectionAssert.AreEqual(new[] { "Nested value is negative" }, Texts("nested.value"));
        }

        [Test]
        public void HiddenFields_ShowNoMessages_UntilTheyAppear()
        {
            var hidden = NewComponent<HiddenValidationTestComponent>();
            VisualElement hiddenRoot = Inspect(hidden);
            Assert.IsEmpty(hiddenRoot.Q("sm-validation:target").Query<HelpBox>().ToList());

            hidden.needsTarget = true;
            InspectorBuilder.RefresherOf(hiddenRoot).Refresh();

            CollectionAssert.AreEqual(new[] { "Target is required" }, hiddenRoot.Q("sm-validation:target").Query<HelpBox>().ToList().Select(box => box.text));
        }

        [Test]
        public void Messages_AreReplaced_NotDuplicated()
        {
            Build();

            refresher.Refresh();
            refresher.Refresh();

            Assert.AreEqual(1, Messages("requiredObject").Length);
        }

        [Test]
        public void Misuse_ShowsProblems_AndAThrowingValidatorBecomesAnError()
        {
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: validator boom"));

            VisualElement misused = Inspect(NewComponent<MisusedValidationTestComponent>());

            Assert.AreEqual("Required: only works on object references, strings and [SerializeReference] fields", misused.Q("sm-property:notAReference").Q<HelpBox>().text);
            Assert.AreEqual("MinValue: only works on numbers and vectors", misused.Q("sm-property:notANumber").Q<HelpBox>().text);
            Assert.AreEqual("ValidateInputHandler failed: validator boom", misused.Q("sm-validation:exploding").Q<HelpBox>().text);
        }
    }
}
