using System;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AttributeHandlerAttribute : Attribute
    {
        public AttributeHandlerAttribute(Type attributeType) => AttributeType = attributeType;

        public Type AttributeType { get; }
    }

    public abstract class AttributeHandler
    {
        public InspectorProperty Property { get; private set; }

        internal void Initialize(InspectorProperty property, InspectorAttribute attribute)
        {
            Property = property;
            SetAttribute(attribute);
        }

        internal abstract void SetAttribute(InspectorAttribute attribute);
    }

    public abstract class AttributeHandler<TAttribute> : AttributeHandler where TAttribute : InspectorAttribute
    {
        public TAttribute Attribute { get; private set; }

        internal sealed override void SetAttribute(InspectorAttribute attribute) => Attribute = (TAttribute)attribute;
    }

    internal interface IFieldHandler
    {
        int Priority { get; }
        VisualElement CreateField();
    }

    public abstract class FieldHandler<TAttribute> : AttributeHandler<TAttribute>, IFieldHandler where TAttribute : InspectorAttribute
    {
        public virtual int Priority => 0;

        public abstract VisualElement CreateField();
    }
}
