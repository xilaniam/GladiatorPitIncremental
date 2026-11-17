using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    public sealed class InspectorProperty
    {
        readonly List<InspectorProperty> children = new();
        readonly List<AttributeHandler> handlers = new();
        Getter<object> valueGetter;
        bool valueGetterResolved;

        internal InspectorProperty(SerializedProperty serializedProperty, MemberModel member, Type ownerType, InspectorProperty parent, UnityEngine.Object[] targets)
        {
            SerializedProperty = serializedProperty;
            Member = member;
            OwnerType = ownerType;
            Parent = parent;
            Targets = targets;
            parent?.children.Add(this);
        }

        public SerializedProperty SerializedProperty { get; }
        public MemberModel Member { get; }
        public Type OwnerType { get; }
        public InspectorProperty Parent { get; }
        public IReadOnlyList<InspectorProperty> Children => children;
        public UnityEngine.Object[] Targets { get; }
        public VisualElement Element { get; internal set; }
        public string Label { get; internal set; }
        public string Name => SerializedProperty != null ? SerializedProperty.name : Member?.Name;
        public bool IsVisible { get; internal set; } = true;
        public bool IsEnabled { get; internal set; } = true;

        internal IReadOnlyList<AttributeHandler> Handlers => handlers;

        public object GetOwner()
        {
            if (Parent != null) return Parent.GetValue();
            return Targets != null && Targets.Length > 0 ? Targets[0] : null;
        }

        public object GetValue()
        {
            if (!valueGetterResolved)
            {
                valueGetter = OwnerType != null && Member != null ? MemberResolver.Value<object>(OwnerType, Member.Info.Name) : default;
                valueGetterResolved = true;
            }
            object owner = GetOwner();
            return owner == null ? null : valueGetter.Get(owner);
        }

        internal void AddHandler(AttributeHandler handler) => handlers.Add(handler);
    }
}
