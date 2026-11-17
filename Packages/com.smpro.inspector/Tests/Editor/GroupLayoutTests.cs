using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public class GroupLayoutTests : InspectorTestBase
    {
        const string Prefix = "SMInspectorPro/SMPro.Inspector.Tests.LayoutTestComponent/";
        static readonly string[] PreferenceKeys = { Prefix + "Closed/expanded", Prefix + "Open/expanded", Prefix + "Tabs/tab" };

        [SetUp]
        public void ClearPreferences()
        {
            foreach (string key in PreferenceKeys) EditorPrefs.DeleteKey(key);
        }

        [TearDown]
        public void RemovePreferences()
        {
            foreach (string key in PreferenceKeys) EditorPrefs.DeleteKey(key);
        }

        VisualElement Layout() => Inspect(NewComponent<LayoutTestComponent>());

        [Test]
        public void GroupPath_IsNormalized()
        {
            var nested = new BoxGroupAttribute(" A / B/ ");
            var top = new BoxGroupAttribute("A");

            Assert.AreEqual("A/B", nested.Path);
            Assert.AreEqual("B", nested.Name);
            Assert.AreEqual("A", nested.ParentPath);
            Assert.AreEqual(string.Empty, top.ParentPath);
            Assert.AreEqual("Tabs/Combat", new TabGroupAttribute("Tabs", "Combat").PagePath);
        }

        [Test]
        public void TopLevel_FollowsPropertyOrder_AndGroupsSitAtTheirFirstMember()
        {
            CollectionAssert.AreEqual(new[]
            {
                "PropertyField:m_Script",
                "PropertyField:movedFirst",
                "PropertyField:first",
                "sm-group:Stats",
                "PropertyField:between",
                "sm-group:Hidden Title",
                "sm-group:Closed",
                "sm-group:Open",
                "sm-group:Tabs",
                "sm-group:Row",
                "sm-group:Implicit",
                "PropertyField:nested",
                "PropertyField:movedLast"
            }, ChildNames(Layout()));
        }

        [Test]
        public void BoxGroup_HasATitle_AndHoldsItsMembersInOrder()
        {
            VisualElement box = Layout().Q("sm-group:Stats");

            Assert.IsTrue(box.ClassListContains("sm-box"));
            Assert.AreEqual("Stats", box.Q<Label>(className: "sm-box__title").text);
            CollectionAssert.AreEqual(new[] { "PropertyField:health", "PropertyField:armor" }, ChildNames(box.Q(className: "sm-box__body")));
        }

        [Test]
        public void BoxGroup_CanHideItsTitle()
        {
            VisualElement box = Layout().Q("sm-group:Hidden Title");

            Assert.IsNull(box.Q(className: "sm-box__title"));
            Assert.IsNotNull(box.Q("PropertyField:untitled"));
        }

        [Test]
        public void FoldoutGroup_StartsClosed_UnlessExpanded()
        {
            VisualElement root = Layout();

            Assert.IsFalse(root.Q<Foldout>("sm-group:Closed").value);
            Assert.IsTrue(root.Q<Foldout>("sm-group:Open").value);
            Assert.IsNotNull(root.Q<Foldout>("sm-group:Closed").Q("PropertyField:closedValue"));
        }

        [Test]
        public void FoldoutGroup_RestoresItsSavedState()
        {
            EditorPrefs.SetBool(Prefix + "Closed/expanded", true);
            EditorPrefs.SetBool(Prefix + "Open/expanded", false);

            VisualElement root = Layout();

            Assert.IsTrue(root.Q<Foldout>("sm-group:Closed").value);
            Assert.IsFalse(root.Q<Foldout>("sm-group:Open").value);
        }

        [Test]
        public void TabGroup_MakesOneTabPerName_InFirstUseOrder()
        {
            var tabs = Layout().Q<TabView>("sm-group:Tabs");
            Tab[] pages = tabs.Query<Tab>().ToList().ToArray();

            CollectionAssert.AreEqual(new[] { "Combat", "Loot" }, pages.Select(page => page.label).ToArray());
            CollectionAssert.AreEqual(
                new[] { "PropertyField:damage", "PropertyField:range", "sm-group:Tabs/Combat/Crits" },
                ChildNames(pages[0].contentContainer));
            CollectionAssert.AreEqual(new[] { "PropertyField:gold" }, ChildNames(pages[1].contentContainer));
            Assert.IsNotNull(pages[0].Q("sm-group:Tabs/Combat/Crits").Q("PropertyField:critChance"));
        }

        [Test]
        public void TabGroup_RestoresTheSelectedTab()
        {
            EditorPrefs.SetInt(Prefix + "Tabs/tab", 1);

            Assert.AreEqual(1, Layout().Q<TabView>("sm-group:Tabs").selectedTabIndex);
        }

        [Test]
        public void HorizontalGroup_SizesEachCellFromItsWidth()
        {
            VisualElement[] cells = Layout().Q("sm-group:Row").Children().ToArray();

            Assert.AreEqual(4, cells.Length);
            Assert.AreEqual(1f, cells[0].style.flexGrow.value);
            Assert.AreEqual(LengthUnit.Percent, cells[1].style.width.value.unit);
            Assert.AreEqual(30f, cells[1].style.width.value.value, 0.001f);
            Assert.AreEqual(LengthUnit.Pixel, cells[2].style.width.value.unit);
            Assert.AreEqual(80f, cells[2].style.width.value.value);
            Assert.IsNotNull(cells[0].Q("PropertyField:speed"));
            Assert.IsNotNull(cells[1].Q("PropertyField:canDash"));
            Assert.IsNotNull(cells[2].Q("PropertyField:lane"));
        }

        [Test]
        public void VerticalGroup_StacksAColumnInsideARow()
        {
            VisualElement column = Layout().Q("sm-group:Row").Q("sm-group:Row/Column");

            CollectionAssert.AreEqual(new[] { "PropertyField:columnTop", "PropertyField:columnBottom" }, ChildNames(column));
        }

        [Test]
        public void UndeclaredParent_BecomesAPlainContainer()
        {
            VisualElement parent = Layout().Q("sm-group:Implicit");

            Assert.IsNotNull(parent.Q("sm-group:Implicit/Child").Q("PropertyField:implicitChild"));
        }

        [Test]
        public void Groups_WorkInsideANestedClass()
        {
            VisualElement foldout = Layout().Q("PropertyField:nested").Q<Foldout>();

            CollectionAssert.AreEqual(new[] { "sm-group:Inner", "PropertyField:nested.c" }, ChildNames(foldout.contentContainer));
            Assert.IsNotNull(foldout.Q("sm-group:Inner").Q("PropertyField:nested.a"));
            Assert.IsNotNull(foldout.Q("sm-group:Inner").Q("PropertyField:nested.b"));
        }

        [Test]
        public void ConflictingGroupKinds_UseTheFirst_AndWarnOnce()
        {
            LogAssert.Expect(LogType.Warning, "SMInspectorPro: group 'A' on ConflictTestComponent is declared as both BoxGroup and FoldoutGroup; using BoxGroup");

            VisualElement group = Inspect(NewComponent<ConflictTestComponent>()).Q("sm-group:A");
            Inspect(NewComponent<ConflictTestComponent>());

            Assert.IsTrue(group.ClassListContains("sm-box"));
            Assert.IsNotNull(group.Q("PropertyField:boxed"));
            Assert.IsNotNull(group.Q("PropertyField:alsoBoxed"));
        }

        [Test]
        public void Stylesheet_IsAttached()
        {
            Assert.Greater(Layout().styleSheets.count, 0);
        }
    }
}
