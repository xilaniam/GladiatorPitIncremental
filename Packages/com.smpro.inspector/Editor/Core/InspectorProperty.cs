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
        public string Name => SerializedProperty != null ? SerializedProperty.name : Member?.Name;

        internal IReadOnlyList<AttributeHandler> Handlers => handlers;

        internal void AddHandler(AttributeHandler handler) => handlers.Add(handler);
    }
}
