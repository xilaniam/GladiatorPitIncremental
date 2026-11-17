using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.Tests
{
    public class InspectorBuilderTests
    {
        readonly List<Object> created = new();

        [SetUp]
        public void ClearReportedErrors() => InspectorBuilder.ClearReportedErrors();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (Object item in created)
                if (item != null) Object.DestroyImmediate(item);
            created.Clear();
        }

        T NewComponent<T>() where T : Component
        {
            GameObject owner = EditorUtility.CreateGameObjectWithHideFlags(typeof(T).Name, HideFlags.HideAndDontSave, typeof(T));
            created.Add(owner);
            return owner.GetComponent<T>();
        }

        Editor EditorFor(Object target)
        {
            Editor editor = Editor.CreateEditor(target);
            created.Add(editor);
            return editor;
        }

        VisualElement Inspect(Object target) => EditorFor(target).CreateInspectorGUI();

        static string[] FieldNames(VisualElement root) => root.Query<PropertyField>().ToList().Select(field => field.name).ToArray();

        [Test]
        public void PlainComponent_GetsTheFallbackEditor_WithUnityDefaultContents()
        {
            var component = NewComponent<PlainTestComponent>();

            Editor editor = EditorFor(component);
            VisualElement root = editor.CreateInspectorGUI();

            Assert.IsInstanceOf<SMInspectorEditor>(editor);
            Assert.IsNull(root.Q("SMInspectorPro"));
            CollectionAssert.AreEqual(new[] { "PropertyField:m_Script", "PropertyField:health", "PropertyField:speed" }, FieldNames(root));
        }

        [Test]
        public void ComponentWithItsOwnEditor_KeepsIt()
        {
            Assert.IsInstanceOf<CustomEditorTestComponentEditor>(EditorFor(NewComponent<CustomEditorTestComponent>()));
        }

        [Test]
        public void AttributedComponent_IsBuiltField_ByField()
        {
            VisualElement root = Inspect(NewComponent<AttributedTestComponent>());

            Assert.IsNotNull(root.Q("SMInspectorPro"));
            PropertyField script = root.Q<PropertyField>("PropertyField:m_Script");
            Assert.IsNotNull(script);
            Assert.IsFalse(script.enabledSelf);
            Assert.IsNotNull(root.Q<Label>("test-field:marked"));
            Assert.IsNotNull(root.Q<PropertyField>("PropertyField:unmarked"));
            Assert.IsNotNull(root.Q<PropertyField>("PropertyField:unhandled"));
        }

        [Test]
        public void HighestPriorityFieldHandler_Wins()
        {
            VisualElement root = Inspect(NewComponent<AttributedTestComponent>());

            Assert.IsNotNull(root.Q("test-priority:both"));
            Assert.IsNull(root.Q("test-field:both"));
        }

        [Test]
        public void NestedClassWithAttributes_BecomesAFoldoutOfBuiltChildren()
        {
            VisualElement root = Inspect(NewComponent<AttributedTestComponent>());

            VisualElement nested = root.Q("PropertyField:nested");
            Foldout foldout = nested.Q<Foldout>();
            Assert.AreEqual("Nested", foldout.text);
            Assert.IsNotNull(foldout.Q<Label>("test-field:inner"));
            Assert.IsNotNull(foldout.Q<PropertyField>("PropertyField:nested.plainInner"));
        }

        [Test]
        public void NestedClassWithoutAttributes_StaysAPropertyField()
        {
            VisualElement root = Inspect(NewComponent<AttributedTestComponent>());

            Assert.IsNotNull(root.Q<PropertyField>("PropertyField:plain"));
        }

        [Test]
        public void TakenOverNestedField_KeepsHeaderAndTooltip()
        {
            VisualElement headed = Inspect(NewComponent<AttributedTestComponent>()).Q("PropertyField:headed");

            Assert.IsTrue(headed.Query<Label>().ToList().Any(label => label.text == "Stats"));
            Assert.AreEqual("Nested stats", headed.Q<Foldout>().tooltip);
        }

        [Test]
        public void ThrowingHandler_FallsBackToTheDefaultField_WithAMessage()
        {
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            VisualElement root = Inspect(NewComponent<ThrowingTestComponent>());

            VisualElement broken = root.Q("PropertyField:broken");
            Assert.IsNotNull(broken.Q<PropertyField>());
            Assert.AreEqual("TestThrowingHandler failed on 'broken': boom", broken.Q<HelpBox>().text);
            Assert.IsNotNull(root.Q<PropertyField>("PropertyField:healthy"));
        }

        [Test]
        public void ScriptableObject_WithAttributes_IsBuiltToo()
        {
            var asset = ScriptableObject.CreateInstance<AttributedTestAsset>();
            created.Add(asset);

            Editor editor = EditorFor(asset);

            Assert.IsInstanceOf<SMInspectorEditor>(editor);
            Assert.IsNotNull(editor.CreateInspectorGUI().Q<Label>("test-field:marked"));
        }

        [Test]
        public void Build_OfFiftyFields_IsFast()
        {
            const int builds = 20;
            var serializedObject = new SerializedObject(NewComponent<WideTestComponent>());
            InspectorBuilder.Build(serializedObject);
            InspectorElement.FillDefaultInspector(new VisualElement(), serializedObject, null);

            Stopwatch ours = Stopwatch.StartNew();
            for (int i = 0; i < builds; i++) InspectorBuilder.Build(serializedObject);
            ours.Stop();

            Stopwatch unity = Stopwatch.StartNew();
            for (int i = 0; i < builds; i++) InspectorElement.FillDefaultInspector(new VisualElement(), serializedObject, null);
            unity.Stop();

            double oursMs = ours.Elapsed.TotalMilliseconds / builds;
            double unityMs = unity.Elapsed.TotalMilliseconds / builds;
            Debug.Log($"Inspector build, 50 fields: SMInspectorPro {oursMs:0.000} ms, Unity default {unityMs:0.000} ms");
            Assert.Less(oursMs, 8.0);
        }
    }
}
