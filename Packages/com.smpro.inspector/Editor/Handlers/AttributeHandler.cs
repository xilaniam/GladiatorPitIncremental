using System;
using System.Collections.Generic;
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
        readonly List<string> problems = new();

        public InspectorProperty Property { get; private set; }
        public IReadOnlyList<string> Problems => problems;

        internal void Initialize(InspectorProperty property, InspectorAttribute attribute)
        {
            Property = property;
            SetAttribute(attribute);
            OnInitialize();
        }

        internal abstract void SetAttribute(InspectorAttribute attribute);

        protected virtual void OnInitialize()
        {
        }

        protected internal virtual void OnFieldCreated(VisualElement field)
        {
        }

        protected void ReportProblem(string message) => problems.Add(message);

        protected BoundGetter<T> ResolveValue<T>(string path) => BoundGetter<T>.Value(Property, path);

        protected BoundGetter<bool> ResolveCondition(string path, object[] values) => BoundGetter<bool>.Condition(Property, path, values);
    }

    public abstract class AttributeHandler<TAttribute> : AttributeHandler where TAttribute : InspectorAttribute
    {
        public TAttribute Attribute { get; private set; }

        internal sealed override void SetAttribute(InspectorAttribute attribute) => Attribute = (TAttribute)attribute;

        protected string AttributeName
        {
            get
            {
                string name = typeof(TAttribute).Name;
                return name.EndsWith("Attribute", StringComparison.Ordinal) ? name.Substring(0, name.Length - "Attribute".Length) : name;
            }
        }
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

    internal interface IRefreshable
    {
        bool NeedsPolling { get; }
        void Refresh();
    }
}
