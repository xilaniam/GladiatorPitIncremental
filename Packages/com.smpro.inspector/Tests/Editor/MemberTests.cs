using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.Tests
{
    public class MemberTests : InspectorTestBase
    {
        MemberTestComponent component;
        VisualElement root;
        InspectorRefresher refresher;

        [SetUp]
        public void ResetStatics() => MemberTestComponent.staticCalls = 0;

        void Build()
        {
            component = NewComponent<MemberTestComponent>();
            root = Inspect(component);
            refresher = InspectorBuilder.RefresherOf(root);
        }

        T Handler<T>(string path) where T : AttributeHandler => refresher.Find(path).Handlers.OfType<T>().Single();

        [Test]
        public void Button_UsesTheMethodNameOrItsLabel_AndSize()
        {
            Build();

            Assert.AreEqual("Increment", root.Q<Button>("sm-button:Increment").text);
            Button addMany = root.Q<Button>("sm-button:AddMany");
            Assert.AreEqual("Add Many", addMany.text);
            Assert.IsTrue(addMany.ClassListContains("sm-button--large"));
        }

        [Test]
        public void Button_InvokesTheMethod_AndCanBeUndone()
        {
            Build();

            Handler<ButtonHandler>("Increment").Invoke();
            Assert.AreEqual(1, component.counter);

            Undo.PerformUndo();
            Assert.AreEqual(0, component.counter);
        }

        [Test]
        public void Button_RunsOnEveryTarget()
        {
            var first = NewComponent<MemberTestComponent>();
            var second = NewComponent<MemberTestComponent>();
            Editor editor = EditorFor(new Object[] { first, second });
            InspectorRefresher multi = InspectorBuilder.RefresherOf(editor.CreateInspectorGUI());

            multi.Find("Increment").Handlers.OfType<ButtonHandler>().Single().Invoke();

            Assert.AreEqual(1, first.counter);
            Assert.AreEqual(1, second.counter);
        }

        [Test]
        public void Button_DrawsParameterFields_WithTheirDefaults()
        {
            Build();
            var amount = root.Q<IntegerField>("sm-argument:amount");

            Assert.AreEqual(5, amount.value);
            Handler<ButtonHandler>("AddMany").Invoke();
            Assert.AreEqual(5, component.counter);

            amount.value = 3;
            Handler<ButtonHandler>("AddMany").Invoke();
            Assert.AreEqual(8, component.counter);
        }

        [Test]
        public void Button_PassesEnumStringAndBoolArguments()
        {
            Build();
            root.Q<EnumField>("sm-argument:mode").value = MemberTestComponent.Mode.Busy;
            root.Q<TextField>("sm-argument:note").value = "hello";
            root.Q<Toggle>("sm-argument:flag").value = true;

            Handler<ButtonHandler>("Configure").Invoke();

            Assert.AreEqual(MemberTestComponent.Mode.Busy, component.lastMode);
            Assert.AreEqual("hello", component.lastNote);
            Assert.IsTrue(component.lastFlag);
        }

        [Test]
        public void StaticButton_RunsOnce()
        {
            Build();

            Handler<ButtonHandler>("CountStatic").Invoke();

            Assert.AreEqual(1, MemberTestComponent.staticCalls);
        }

        [Test]
        public void ThrowingButton_LogsTheException_WithoutBreaking()
        {
            Build();
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: button boom"));

            Assert.DoesNotThrow(() => Handler<ButtonHandler>("Throws").Invoke());
        }

        [Test]
        public void UnsupportedParameter_DisablesTheButton_WithAnError()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Button: parameter 'values' of type List`1 is not supported"));
            Build();

            VisualElement unsupported = root.Q("sm-property:Unsupported");
            Assert.IsFalse(unsupported.Q<Button>().enabledSelf);
            Assert.AreEqual("Button: parameter 'values' of type List`1 is not supported", unsupported.Q<HelpBox>().text);
        }

        [Test]
        public void Buttons_FollowConditionsAndGroups()
        {
            Build();
            VisualElement conditional = root.Q("sm-button:Conditional");
            Assert.AreEqual(DisplayStyle.None, conditional.style.display.value);

            component.showConditional = true;
            refresher.Refresh();
            Assert.AreNotEqual(DisplayStyle.None, conditional.style.display.value);

            Assert.IsNotNull(root.Q("sm-group:Actions").Q<Button>("sm-button:Grouped"));
        }

        [Test]
        public void NestedClassButton_RunsOnTheNestedObject()
        {
            Build();

            Handler<ButtonHandler>("nested.Bump").Invoke();

            Assert.AreEqual(1, component.nested.bumps);
        }

        [Test]
        public void ShowInInspector_ShowsAReadOnlyValue_AndRefreshes()
        {
            Build();
            var doubled = root.Q("sm-member:Doubled").Q<IntegerField>();
            Assert.IsFalse(doubled.enabledSelf);
            Assert.AreEqual(0, doubled.value);

            component.counter = 21;
            refresher.Refresh();

            Assert.AreEqual(42, doubled.value);
            Assert.IsTrue(refresher.Polls);
        }

        [Test]
        public void ShowInInspector_ShowsPrivateFields_AndFallsBackToText()
        {
            Build();

            Assert.AreEqual("idle", root.Q("sm-member:status").Q<TextField>().value);
            StringAssert.Contains("Dictionary", root.Q("sm-member:complex").Q<TextField>().value);
        }

        [Test]
        public void ShowInInspector_OnASerializedField_KeepsTheNormalField()
        {
            Build();

            Assert.IsNotNull(root.Q<PropertyField>("PropertyField:serializedAndShown"));
        }

        [Test]
        public void Members_ComeAfterTheFieldsOfTheirClass()
        {
            Build();
            string[] names = root.Children().Select(child => child.name).ToArray();

            Assert.Less(Array.IndexOf(names, "PropertyField:nested"), Array.IndexOf(names, "sm-member:status"));
            Assert.Less(Array.IndexOf(names, "sm-member:Doubled"), Array.IndexOf(names, "sm-button:Increment"));
        }

        [Test]
        public void OnValueChanged_CallsTheMethod_WithOrWithoutTheValue()
        {
            Build();
            component.watchedValue = 2.5f;

            Handler<OnValueChangedHandler>("watched").Invoke();
            Handler<OnValueChangedHandler>("watchedValue").Invoke();

            Assert.AreEqual(1, component.changedCalls);
            Assert.AreEqual(2.5f, component.lastValue);
        }

        [UnityTest]
        public IEnumerator LiveInspector_CallsOnValueChanged_WhenTheFieldChanges()
        {
            var target = NewComponent<MemberTestComponent>();
            var window = ScriptableObject.CreateInstance<InspectorTestWindow>();
            window.Show();
            try
            {
                var inspector = new InspectorElement(target);
                window.rootVisualElement.Add(inspector);
                yield return WaitUntil(() => inspector.Q("PropertyField:watched") != null);
                yield return null;

                var serializedObject = new SerializedObject(target);
                serializedObject.FindProperty(nameof(MemberTestComponent.watched)).intValue = 7;
                serializedObject.ApplyModifiedProperties();

                yield return WaitUntil(() => target.changedCalls > 0);
                Assert.AreEqual(1, target.changedCalls);
            }
            finally
            {
                window.Close();
            }
        }

        static IEnumerator WaitUntil(Func<bool> condition)
        {
            double deadline = EditorApplication.timeSinceStartup + 5.0;
            while (!condition() && EditorApplication.timeSinceStartup < deadline) yield return null;
        }
    }
}
