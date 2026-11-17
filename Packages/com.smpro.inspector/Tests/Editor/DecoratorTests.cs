using System.Linq;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    public class DecoratorTests : InspectorTestBase
    {
        VisualElement Root() => Inspect(NewComponent<DecoratorTestComponent>());

        static string[] Texts(VisualElement element) => element.Query<Label>().ToList().Select(label => label.text).ToArray();

        [Test]
        public void Title_SitsAboveTheField_WithSubtitleAndLine()
        {
            VisualElement damage = Root().Q("sm-property:damage");
            VisualElement[] parts = damage.Children().ToArray();

            Assert.IsTrue(parts[0].ClassListContains("sm-title"));
            CollectionAssert.AreEqual(new[] { "Combat", "Damage settings" }, Texts(parts[0]));
            Assert.IsNotNull(parts[0].Q(className: "sm-title__line"));
            Assert.AreEqual("PropertyField:damage", parts[1].name);
        }

        [Test]
        public void Title_CanLeaveOutTheLine()
        {
            Assert.IsNull(Root().Q("sm-property:plain").Q(className: "sm-title__line"));
        }

        [Test]
        public void InfoBox_ShowsItsMessageAndType()
        {
            HelpBox box = Root().Q("sm-property:risky").Q<HelpBox>();

            Assert.AreEqual("Careful", box.text);
            Assert.AreEqual(HelpBoxMessageType.Warning, box.messageType);
        }

        [Test]
        public void LabelText_RenamesTheField()
        {
            Assert.AreEqual("Move Speed", Root().Q<PropertyField>("PropertyField:speed").label);
        }

        [Test]
        public void HideLabel_ClearsTheLabel()
        {
            Assert.AreEqual(string.Empty, Root().Q<PropertyField>("PropertyField:note").label);
        }

        [Test]
        public void UndecoratedField_KeepsItsDisplayName()
        {
            Assert.AreEqual("Damage", Root().Q<PropertyField>("PropertyField:damage").label);
        }

        [Test]
        public void Suffix_SitsAfterTheFieldInARow()
        {
            VisualElement row = Root().Q("sm-property:cooldown");
            VisualElement[] parts = row.Children().ToArray();

            Assert.IsTrue(row.ClassListContains("sm-suffix"));
            Assert.AreEqual("PropertyField:cooldown", parts[0].name);
            Assert.AreEqual("s", ((Label)parts[1]).text);
        }

        [Test]
        public void Indent_AddsAMarginPerLevel()
        {
            VisualElement field = Root().Q<PropertyField>("PropertyField:indented");

            Assert.AreEqual(IndentHandler.StepWidth * 2, field.style.marginLeft.value.value);
        }

        [Test]
        public void Decorators_StackInAFixedOrder_WhateverTheDeclarationOrder()
        {
            VisualElement stacked = Root().Q("sm-property:stacked");
            VisualElement[] outer = stacked.Children().ToArray();
            VisualElement[] inner = outer[1].Children().ToArray();
            VisualElement[] row = inner[1].Children().ToArray();

            Assert.IsTrue(outer[0].ClassListContains("sm-title"));
            Assert.IsInstanceOf<HelpBox>(inner[0]);
            Assert.IsTrue(inner[1].ClassListContains("sm-suffix"));
            Assert.AreEqual("PropertyField:stacked", row[0].name);
            Assert.AreEqual("m", ((Label)row[1]).text);
        }

        [Test]
        public void DecoratedField_StillGoesIntoItsGroup()
        {
            VisualElement body = Root().Q("sm-group:Box").Q(className: "sm-box__body");

            Assert.AreEqual("sm-property:boxed", body.Children().Single().name);
        }

        [Test]
        public void Decorators_WrapCustomFieldHandlers()
        {
            VisualElement custom = Root().Q("sm-property:custom");

            Assert.IsNotNull(custom.Q(className: "sm-title"));
            Assert.IsNotNull(custom.Q<Label>("test-field:custom"));
        }

        [Test]
        public void HideLabel_OnANestedClass_DrawsItsFieldsInline()
        {
            VisualElement inline = Root().Q("PropertyField:inline");

            Assert.IsNull(inline.Q<Foldout>());
            Assert.IsNotNull(inline.Q<Label>("test-field:x"));
            Assert.IsNotNull(inline.Q<PropertyField>("PropertyField:inline.y"));
        }

        [Test]
        public void LabelText_OnANestedClass_RenamesTheFoldout()
        {
            Assert.AreEqual("Renamed Nested", Root().Q("PropertyField:renamedNested").Q<Foldout>().text);
        }
    }
}
