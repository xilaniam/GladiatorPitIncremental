using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(TypePickerAttribute))]
    public sealed class TypePickerHandler : DecoratorHandler<TypePickerAttribute>
    {
        public const string None = "None";

        readonly List<Type> types = new();
        string title;

        public override int Order => 15;
        public IReadOnlyList<Type> Types => types;
        public DropdownField Dropdown { get; private set; }

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty?.propertyType != SerializedPropertyType.ManagedReference)
            {
                ReportProblem("TypePicker: only works on [SerializeReference] fields");
                return;
            }
            title = Property.Label;
            Property.Label = string.Empty;
            types.AddRange(AssignableTypes(Property.Member.ValueType));
        }

        public static List<Type> AssignableTypes(Type baseType)
        {
            var found = new List<Type>();
            if (IsCandidate(baseType)) found.Add(baseType);
            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
                if (IsCandidate(type)) found.Add(type);
            found.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return found;
        }

        static bool IsCandidate(Type type) =>
            !type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters &&
            !typeof(Object).IsAssignableFrom(type) && type.IsDefined(typeof(SerializableAttribute), false) &&
            (type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null);

        public override VisualElement Decorate(VisualElement field)
        {
            if (Problems.Count > 0) return field;

            var choices = new List<string> { None };
            foreach (Type type in types) choices.Add(DisplayName(type));
            Type current = Property.SerializedProperty.managedReferenceValue?.GetType();
            int index = current == null ? 0 : types.IndexOf(current) + 1;

            Dropdown = new DropdownField(title, choices, Math.Max(index, 0)) { name = "sm-type-picker:" + Property.Path };
            Dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            if (current != null && index <= 0) Dropdown.SetValueWithoutNotify(DisplayName(current));
            Dropdown.RegisterValueChangedCallback(change =>
            {
                if (change.target != Dropdown) return;
                int chosen = Dropdown.index;
                Select(chosen <= 0 ? null : types[chosen - 1]);
            });

            field.AddToClassList("sm-type-picker__value");
            return Stack(Dropdown, field);
        }

        string DisplayName(Type type)
        {
            string name = ObjectNames.NicifyVariableName(type.Name);
            int sameName = 0;
            foreach (Type other in types)
                if (other.Name == type.Name) sameName++;
            return sameName > 1 ? $"{name} ({type.Namespace})" : name;
        }

        public void Select(Type type)
        {
            SerializedProperty property = Property.SerializedProperty;
            if (property.managedReferenceValue?.GetType() == type) return;
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            property.serializedObject.ApplyModifiedProperties();
            RequestRebuild();
        }
    }
}
