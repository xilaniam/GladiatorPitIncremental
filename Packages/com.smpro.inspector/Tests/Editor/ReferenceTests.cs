using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public class ReferenceTests : InspectorTestBase
    {
        [Serializable]
        public class Holder
        {
            public SerializableDictionary<string, int> map = new();
        }

        ReferenceTestComponent component;
        VisualElement root;

        void Build()
        {
            component = NewComponent<ReferenceTestComponent>();
            root = Inspect(component);
        }

        InspectorRefresher Refresher => InspectorBuilder.RefresherOf(root);

        T Handler<T>(string path) where T : AttributeHandler => Refresher.Find(path).Handlers.OfType<T>().Single();

        string[] Messages(string path) => root.Q("sm-validation:" + path).Query<HelpBox>().ToList().Select(box => box.text).ToArray();

        [Test]
        public void Dictionary_IsDrawnAsAKeyValueTable()
        {
            Build();
            VisualElement table = root.Q("sm-dictionary:scores");

            CollectionAssert.AreEqual(new[] { "Key", "Value" },
                table.Query<Label>(className: "sm-table__title").ToList().Select(label => label.text));
            Assert.IsNull(table.Q("sm-table-row:0"));
        }

        [Test]
        public void Dictionary_ReportsDuplicateKeys_UntilTheyAreFixed()
        {
            Build();
            ((TableDrawer)root.Q("sm-dictionary:scores").userData).Editor.Add();
            ((TableDrawer)root.Q("sm-dictionary:scores").userData).Editor.Add();

            CollectionAssert.AreEqual(new[] { "Duplicate key '' in entries 0 and 1; only the first is used" }, Messages("scores"));
            Assert.AreEqual(1, component.scores.Count);

            var serializedObject = new SerializedObject(component);
            serializedObject.FindProperty("scores.entries.Array.data[1].key").stringValue = "b";
            serializedObject.FindProperty("scores.entries.Array.data[1].value").intValue = 7;
            serializedObject.ApplyModifiedProperties();
            InspectorView.Of(root).Refresh();

            Assert.IsEmpty(Messages("scores"));
            Assert.AreEqual(7, component.scores["b"]);
            Assert.AreEqual(2, component.scores.Count);
        }

        [Test]
        public void SerializableDictionary_RoundTrips_AndKeepsCodeChanges()
        {
            var holder = new Holder();
            holder.map.Add("a", 1);

            Holder copy = JsonUtility.FromJson<Holder>(JsonUtility.ToJson(holder));
            Assert.AreEqual(1, copy.map["a"]);

            copy.map.Remove("a");
            copy.map["b"] = 2;
            Holder second = JsonUtility.FromJson<Holder>(JsonUtility.ToJson(copy));

            Assert.IsFalse(second.map.ContainsKey("a"));
            Assert.AreEqual(2, second.map["b"]);
        }

        [Test]
        public void TypePicker_ListsConcreteSerializableTypes()
        {
            Build();
            var handler = Handler<TypePickerHandler>("shape");

            CollectionAssert.AreEqual(new[] { typeof(Circle), typeof(Square) }, handler.Types);
            CollectionAssert.AreEqual(new[] { "None", "Circle", "Square" }, handler.Dropdown.choices);
            Assert.AreEqual("None", handler.Dropdown.value);
            CollectionAssert.AreEqual(new[] { typeof(Triangle) }, Handler<TypePickerHandler>("baseShape").Types);
        }

        [Test]
        public void TypePicker_CreatesTheChosenType_AndItsAttributesWork()
        {
            Build();

            Handler<TypePickerHandler>("shape").Select(typeof(Square));

            Assert.IsInstanceOf<Square>(component.shape);
            Assert.AreEqual("Square", Handler<TypePickerHandler>("shape").Dropdown.value);
            Assert.AreEqual(DisplayStyle.None, Refresher.Find("shape.corner").Element.style.display.value);

            ((Square)component.shape).size = -1f;
            InspectorView.Of(root).Refresh();
            Assert.AreEqual(0f, ((Square)component.shape).size);
        }

        [Test]
        public void TypePicker_None_ClearsTheValue()
        {
            Build();
            Handler<TypePickerHandler>("shape").Select(typeof(Circle));

            Handler<TypePickerHandler>("shape").Select(null);

            Assert.IsNull(component.shape);
        }

        [Test]
        public void TypeChangedElsewhere_RebuildsTheInspector()
        {
            Build();

            component.shape = new Square();
            InspectorView.Of(root).Refresh();

            Assert.IsNotNull(Refresher.Find("shape.corner"));
        }

        [Test]
        public void Misuse_ShowsProblems()
        {
            Build();

            Assert.AreEqual("TypePicker: only works on [SerializeReference] fields", root.Q("sm-property:notAReference").Q<HelpBox>().text);
            Assert.AreEqual("InlineEditor: only works on object references", root.Q("sm-property:notAnObject").Q<HelpBox>().text);
        }

        [Test]
        public void InlineEditor_IsHiddenWhenEmpty_AndWontShowItsOwnObject()
        {
            Build();
            Assert.AreEqual(DisplayStyle.None, Handler<InlineEditorHandler>("settings").Box.style.display.value);

            var asset = NewAsset<InlineTestAsset>();
            asset.next = asset;
            VisualElement assetRoot = Inspect(asset);

            Assert.AreEqual("An object can't be shown inside its own inspector", assetRoot.Q("sm-inline:next").Q<HelpBox>().text);
        }

        [UnityTest]
        public IEnumerator InlineEditor_ShowsTheReferencedObjectsInspector()
        {
            var target = NewComponent<ReferenceTestComponent>();
            target.settings = NewAsset<InlineTestAsset>();
            var window = ScriptableObject.CreateInstance<InspectorTestWindow>();
            window.Show();
            try
            {
                var inspector = new InspectorElement(target);
                window.rootVisualElement.Add(inspector);
                yield return WaitUntil(() => inspector.Q("sm-inline:settings")?.Q("PropertyField:level") != null);

                Assert.IsNotNull(inspector.Q("sm-inline:settings").Q<InspectorElement>());
                Assert.IsNotNull(inspector.Q("sm-inline:settings").Q("PropertyField:level"));
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
