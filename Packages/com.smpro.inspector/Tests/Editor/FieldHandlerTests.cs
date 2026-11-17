using System.IO;
using System.Linq;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public class FieldHandlerTests : InspectorTestBase
    {
        FieldTestComponent component;
        VisualElement root;
        InspectorRefresher refresher;

        void Build()
        {
            component = NewComponent<FieldTestComponent>();
            root = Inspect(component);
            refresher = InspectorBuilder.RefresherOf(root);
        }

        T Handler<T>(string path) where T : AttributeHandler => refresher.Find(path).Handlers.OfType<T>().Single();

        [Test]
        public void ValueDropdown_ListsValues_AndShowsTheCurrentOne()
        {
            Build();
            DropdownField dropdown = root.Q<DropdownField>("sm-dropdown:size");

            CollectionAssert.AreEqual(new[] { "1", "2", "4" }, dropdown.choices);
            Assert.AreEqual("2", dropdown.value);
            Assert.AreEqual("Size", dropdown.label);
        }

        [Test]
        public void ValueDropdown_WritesTheChosenValue()
        {
            Build();

            Handler<ValueDropdownHandler>("size").Select(2);

            Assert.AreEqual(4, component.size);
            Assert.AreEqual("4", root.Q<DropdownField>("sm-dropdown:size").value);
        }

        [Test]
        public void ValueDropdown_UsesItemTexts()
        {
            Build();
            DropdownField dropdown = root.Q<DropdownField>("sm-dropdown:speed");
            Assert.AreEqual("Normal", dropdown.value);

            Handler<ValueDropdownHandler>("speed").Select(2);

            Assert.AreEqual(2f, component.speed);
            Assert.AreEqual("Fast", dropdown.value);
        }

        [Test]
        public void ValueDropdown_ReadsAListField_AndShowsUnknownValuesAsText()
        {
            Build();
            CollectionAssert.AreEqual(new[] { "a", "b" }, root.Q<DropdownField>("sm-dropdown:label").choices);

            component.size = 3;
            refresher.Refresh();

            Assert.AreEqual("3", root.Q<DropdownField>("sm-dropdown:size").value);
        }

        [Test]
        public void EnumToggleButtons_SelectOneValue()
        {
            Build();
            Button fire = root.Q<Button>("sm-enum:Fire");
            Button water = root.Q<Button>("sm-enum:Water");
            Assert.IsTrue(fire.ClassListContains(EnumToggleButtonsHandler.SelectedClass));

            Handler<EnumToggleButtonsHandler>("element").Click((long)FieldTestComponent.Element.Water);

            Assert.AreEqual(FieldTestComponent.Element.Water, component.element);
            Assert.IsFalse(fire.ClassListContains(EnumToggleButtonsHandler.SelectedClass));
            Assert.IsTrue(water.ClassListContains(EnumToggleButtonsHandler.SelectedClass));
        }

        [Test]
        public void EnumToggleButtons_ToggleFlags()
        {
            Build();
            var handler = Handler<EnumToggleButtonsHandler>("tags");
            Assert.IsNull(root.Q("sm-enum-buttons:tags").Q<Button>("sm-enum:None"));

            handler.Click((long)FieldTestComponent.Tags.Red);
            handler.Click((long)FieldTestComponent.Tags.Blue);
            Assert.AreEqual(FieldTestComponent.Tags.Red | FieldTestComponent.Tags.Blue, component.tags);

            handler.Click((long)FieldTestComponent.Tags.Red);
            Assert.AreEqual(FieldTestComponent.Tags.Blue, component.tags);
        }

        [Test]
        public void ProgressBar_ShowsAndSetsTheValue()
        {
            Build();
            var handler = Handler<ProgressBarHandler>("health");
            Assert.AreEqual(50f, handler.Bar.value);
            Assert.AreEqual("50", handler.Bar.title);

            handler.SetFromRatio(0.25);
            Assert.AreEqual(25, component.health);

            component.health = 75;
            refresher.Refresh();
            Assert.AreEqual(75f, handler.Bar.value);
        }

        [Test]
        public void ProgressBar_ReadsItsMaximumFromAMember()
        {
            Build();
            var handler = Handler<ProgressBarHandler>("energy");

            Assert.AreEqual(20f, handler.Bar.highValue);
            Assert.AreEqual(5f, handler.Bar.value);

            handler.SetFromRatio(1.5);
            Assert.AreEqual(20f, component.energy);
        }

        [Test]
        public void MinMaxSlider_ShowsAndClampsTheRange()
        {
            Build();
            var handler = Handler<MinMaxSliderHandler>("range");
            Assert.AreEqual(new Vector2(2f, 8f), handler.Slider.value);
            Assert.AreEqual(10f, handler.Slider.highLimit);

            handler.Set(new Vector2(12f, 6f));

            Assert.AreEqual(new Vector2(6f, 10f), component.range);
        }

        [Test]
        public void MinMaxSlider_RoundsIntegerRanges()
        {
            Build();

            Handler<MinMaxSliderHandler>("intRange").Set(new Vector2(10.4f, 19.6f));

            Assert.AreEqual(new Vector2Int(10, 20), component.intRange);
        }

        [Test]
        public void PreviewField_ShowsTheFieldAndAPreviewOfTheChosenSize()
        {
            Build();
            var handler = Handler<PreviewFieldHandler>("icon");

            Assert.IsNotNull(root.Q<PropertyField>("PropertyField:icon"));
            Assert.AreEqual(80f, handler.Preview.style.height.value.value);
            Assert.IsNull(handler.Preview.image);
        }

        [Test]
        public void PathFields_AddABrowseButton()
        {
            Build();

            Assert.IsNotNull(root.Q("sm-path:file").Q<PropertyField>("PropertyField:file"));
            Assert.IsNotNull(root.Q<Button>("sm-browse:file"));
            Assert.IsNotNull(root.Q<Button>("sm-browse:folder"));
        }

        [Test]
        public void PathsInsideTheProject_BecomeRelative()
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            Assert.AreEqual("Assets/Data/a.json", FilePathHandler.ToProjectPath(Path.Combine(project, "Assets", "Data", "a.json")));
            string outside = Path.GetFullPath(Path.Combine(project, "..", "elsewhere.txt")).Replace('\\', '/');
            Assert.AreEqual(outside, FilePathHandler.ToProjectPath(outside));
        }

        [Test]
        public void WrongFieldTypes_FallBackToTheDefaultField_WithAProblem()
        {
            VisualElement misused = Inspect(NewComponent<MisusedFieldTestComponent>());

            string[] problems = misused.Query<HelpBox>().ToList().Select(box => box.text).ToArray();
            CollectionAssert.AreEquivalent(new[]
            {
                "ValueDropdown: No member 'missing' on MisusedFieldTestComponent",
                "EnumToggleButtons: only works on enum fields",
                "ProgressBar: only works on numbers",
                "MinMaxSlider: only works on Vector2 and Vector2Int fields",
                "PreviewField: only works on object references",
                "FilePath: only works on string fields"
            }, problems);
            Assert.IsNotNull(misused.Q<PropertyField>("PropertyField:notAnEnum"));
        }
    }
}
