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
    public class ListTests : InspectorTestBase
    {
        ListTestComponent component;
        VisualElement root;

        void Build()
        {
            component = NewComponent<ListTestComponent>();
            root = Inspect(component);
        }

        InspectorRefresher Refresher => InspectorBuilder.RefresherOf(root);

        ListDrawer List(string path)
        {
            for (VisualElement element = root.Q("sm-list:" + path); element != null; element = element.parent)
                if (element.userData is ListDrawer drawer) return drawer;
            return null;
        }

        TableDrawer Table(string path) => (TableDrawer)root.Q("sm-table:" + path).userData;

        static bool Shown(VisualElement element) => element.style.display.value != DisplayStyle.None;

        [Test]
        public void PlainList_StaysAUnityField()
        {
            Build();

            Assert.IsNotNull(root.Q<PropertyField>("PropertyField:plainNumbers"));
            Assert.IsNull(root.Q("sm-list:plainNumbers"));
        }

        [Test]
        public void ListOfAttributedClasses_IsTakenOver_WithOneItemPerElement()
        {
            Build();
            ListView view = root.Q<ListView>("sm-list:enemies");

            Assert.AreEqual(2, view.itemsSource.Count);
            Assert.AreEqual("Enemies (2)", root.Q<Foldout>("sm-list-foldout:enemies").text);
        }

        [Test]
        public void AttributesInsideElements_Work()
        {
            Build();

            Assert.IsFalse(Shown(Refresher.Find("enemies.Array.data[0].bossHealth").Element));
            Assert.IsTrue(Shown(Refresher.Find("enemies.Array.data[1].bossHealth").Element));
            CollectionAssert.AreEqual(new[] { "Prefab is required" },
                Refresher.Find("enemies.Array.data[0].prefab").Element.Q("sm-validation:enemies.Array.data[0].prefab").Query<HelpBox>().ToList().Select(box => box.text));

            component.enemies[0].boss = true;
            Refresher.Refresh();
            Assert.IsTrue(Shown(Refresher.Find("enemies.Array.data[0].bossHealth").Element));
        }

        [Test]
        public void ElementLabel_ComesFromAMember()
        {
            Build();

            Assert.AreEqual("Bat", Refresher.Find("named.Array.data[0]").Element.Q<Foldout>().text);

            component.named[0].name = "Wolf";
            Refresher.Refresh();
            Assert.AreEqual("Wolf", Refresher.Find("named.Array.data[0]").Element.Q<Foldout>().text);
        }

        [Test]
        public void ListSettings_ControlDraggingAndButtons()
        {
            Build();
            ListView enemies = root.Q<ListView>("sm-list:enemies");
            ListView numbers = root.Q<ListView>("sm-list:fixedNumbers");

            Assert.IsTrue(enemies.reorderable);
            Assert.IsTrue(enemies.showAddRemoveFooter);
            Assert.IsFalse(numbers.reorderable);
            Assert.IsFalse(numbers.showAddRemoveFooter);
        }

        [Test]
        public void AddingRemovingAndMoving_UpdateTheDataAndRebuild()
        {
            Build();

            List("enemies").Editor.Add();
            Assert.AreEqual(3, component.enemies.Count);
            Assert.AreEqual(3, root.Q<ListView>("sm-list:enemies").itemsSource.Count);

            List("enemies").Editor.Move(0, 2);
            Assert.AreEqual("Orc", component.enemies[0].name);

            List("enemies").Editor.Remove(0);
            Assert.AreEqual(2, component.enemies.Count);
            Assert.AreEqual(2, root.Q<ListView>("sm-list:enemies").itemsSource.Count);
        }

        [Test]
        public void ChangesMadeElsewhere_RebuildTheList()
        {
            Build();
            InspectorView view = InspectorView.Of(root);

            component.enemies.Add(new ListTestComponent.Enemy { name = "Troll" });
            view.Refresh();

            Assert.AreEqual(3, root.Q<ListView>("sm-list:enemies").itemsSource.Count);
            Assert.IsNotNull(Refresher.Find("enemies.Array.data[2].bossHealth"));
        }

        [Test]
        public void Undo_RestoresTheListAndRebuilds()
        {
            Build();
            List("enemies").Editor.Add();
            Assert.AreEqual(3, component.enemies.Count);

            Undo.PerformUndo();
            InspectorView.Of(root).Refresh();

            Assert.AreEqual(2, component.enemies.Count);
            Assert.AreEqual(2, root.Q<ListView>("sm-list:enemies").itemsSource.Count);
        }

        [Test]
        public void TableList_HasAHeaderAndARowPerElement()
        {
            Build();
            VisualElement table = root.Q("sm-table:rows");

            CollectionAssert.AreEqual(new[] { "Id", "Label", "Weight" },
                table.Q(className: "sm-table__header").Query<Label>(className: "sm-table__title").ToList().Select(label => label.text));
            Assert.IsNotNull(table.Q("sm-table-row:1"));
            Assert.AreEqual(string.Empty, table.Q<PropertyField>("PropertyField:rows.Array.data[0].id").label);
        }

        [Test]
        public void TableCells_UseTheirAttributes()
        {
            Build();

            component.rows[1].weight = -3f;
            Refresher.Refresh();

            Assert.AreEqual(0f, component.rows[1].weight);
        }

        [Test]
        public void TableList_AddsAndRemovesRows()
        {
            Build();

            Table("rows").Editor.Add();
            Assert.AreEqual(3, component.rows.Count);
            Assert.IsNotNull(root.Q("sm-table:rows").Q("sm-table-row:2"));

            Table("rows").Editor.Remove(0);
            Assert.AreEqual(2, component.rows.Count);
            Assert.AreEqual(2, component.rows[0].id);
        }

        [UnityTest]
        public IEnumerator LiveInspector_RendersElements_AndRebuildsAfterAdding()
        {
            var target = NewComponent<ListTestComponent>();
            var window = ScriptableObject.CreateInstance<InspectorTestWindow>();
            window.Show();
            try
            {
                var inspector = new InspectorElement(target);
                window.rootVisualElement.Add(inspector);
                yield return WaitUntil(() => inspector.Q<Foldout>("sm-list-foldout:enemies") != null);
                inspector.Q<Foldout>("sm-list-foldout:enemies").value = true;
                yield return WaitUntil(() => inspector.Q("PropertyField:enemies.Array.data[1].bossHealth") != null);
                Assert.IsNotNull(inspector.Q("PropertyField:enemies.Array.data[1].bossHealth"), "list items were not rendered");

                root = inspector;
                List("enemies").Editor.Add();
                yield return WaitUntil(() => inspector.Q<ListView>("sm-list:enemies")?.itemsSource.Count == 3);

                Assert.AreEqual(3, target.enemies.Count);
                Assert.AreEqual(3, inspector.Q<ListView>("sm-list:enemies").itemsSource.Count);
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

        [Test]
        public void TableList_OnSimpleValues_FallsBackToAList_WithAProblem()
        {
            Build();
            VisualElement notATable = root.Q("sm-property:notATable");

            Assert.IsNotNull(notATable.Q<ListView>("sm-list:notATable"));
            Assert.AreEqual("TableList: only works on lists of serializable classes or structs", notATable.Q<HelpBox>().text);
        }
    }
}
