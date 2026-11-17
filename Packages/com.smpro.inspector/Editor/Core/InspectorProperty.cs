using System;
using System.Collections;
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
        internal VisualElement ValidationSlot { get; set; }
        public string Name => SerializedProperty != null ? SerializedProperty.name : Member?.Name;
        public string Path => SerializedProperty != null ? SerializedProperty.propertyPath : Parent != null ? Parent.Path + "." + Name : Name;
        public int TargetCount => Targets?.Length ?? 0;
        public bool IsVisible { get; internal set; } = true;
        public bool IsEnabled { get; internal set; } = true;

        internal IReadOnlyList<AttributeHandler> Handlers => handlers;
        internal List<string> BuilderProblems { get; } = new();

        internal void AddBuilderProblem(string problem) => BuilderProblems.Add(problem);

        public object GetOwner(int targetIndex = 0)
        {
            if (Parent != null) return Parent.GetValue(targetIndex);
            return Targets != null && targetIndex < Targets.Length ? Targets[targetIndex] : null;
        }

        public int ElementIndex { get; internal set; } = -1;

        public object GetValue(int targetIndex = 0)
        {
            if (ElementIndex >= 0)
            {
                IList list = Parent?.GetValue(targetIndex) as IList;
                return list != null && ElementIndex < list.Count ? list[ElementIndex] : null;
            }
            if (!valueGetterResolved)
            {
                valueGetter = OwnerType != null && Member != null && Member.Kind != MemberKind.Method ? MemberResolver.Value<object>(OwnerType, Member.Info.Name) : default;
                valueGetterResolved = true;
            }
            object owner = GetOwner(targetIndex);
            return owner == null ? null : valueGetter.Get(owner);
        }

        internal void AddHandler(AttributeHandler handler) => handlers.Add(handler);
    }
}
