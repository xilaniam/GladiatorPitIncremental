using System;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEngine.UIElements;

namespace SMPro.Inspector.Tests
{
    [AttributeHandler(typeof(TestFieldAttribute))]
    public sealed class TestFieldHandler : FieldHandler<TestFieldAttribute>
    {
        public override VisualElement CreateField() => new Label(Property.Name) { name = "test-field:" + Property.Name };
    }

    [AttributeHandler(typeof(TestPriorityAttribute))]
    public sealed class TestPriorityHandler : FieldHandler<TestPriorityAttribute>
    {
        public override int Priority => 5;

        public override VisualElement CreateField() => new Label(Property.Name) { name = "test-priority:" + Property.Name };
    }

    [AttributeHandler(typeof(TestThrowingAttribute))]
    public sealed class TestThrowingHandler : FieldHandler<TestThrowingAttribute>
    {
        public override VisualElement CreateField() => throw new InvalidOperationException("boom");
    }

    [AttributeHandler(typeof(TestThrowingDecoratorAttribute))]
    public sealed class TestThrowingDecoratorHandler : DecoratorHandler<TestThrowingDecoratorAttribute>
    {
        public override VisualElement Decorate(VisualElement field) => throw new InvalidOperationException("decorator boom");
    }

    [CustomEditor(typeof(CustomEditorTestComponent))]
    public sealed class CustomEditorTestComponentEditor : Editor
    {
    }
}
