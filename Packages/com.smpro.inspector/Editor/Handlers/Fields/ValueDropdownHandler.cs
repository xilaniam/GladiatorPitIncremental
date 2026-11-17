using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(ValueDropdownAttribute))]
    public sealed class ValueDropdownHandler : FieldHandler<ValueDropdownAttribute>, IRefreshable
    {
        readonly List<ValueDropdownItem> options = new();
        BoundGetter<IEnumerable> source;
        DropdownField dropdown;

        public bool NeedsPolling => false;
        public IReadOnlyList<ValueDropdownItem> Options => options;
        public DropdownField Dropdown => dropdown;

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty == null)
            {
                ReportProblem("ValueDropdown: only works on serialized fields");
                return;
            }
            source = ResolveValue<IEnumerable>(Attribute.Member);
            if (!source.IsValid) ReportProblem($"ValueDropdown: {source.Error}");
        }

        public override VisualElement CreateField()
        {
            if (Problems.Count > 0) return null;
            dropdown = new DropdownField(Property.Label) { name = "sm-dropdown:" + Property.Path };
            dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            LoadOptions();
            dropdown.RegisterCallback<PointerDownEvent>(_ => LoadOptions(), TrickleDown.TrickleDown);
            dropdown.RegisterValueChangedCallback(change =>
            {
                if (change.target != dropdown) return;
                int index = dropdown.index;
                if (index >= 0 && index < options.Count) Select(index);
            });
            Refresh();
            return dropdown;
        }

        public void LoadOptions()
        {
            options.Clear();
            IEnumerable items = source.Get();
            if (items != null)
                foreach (object item in items)
                    options.Add(item is ValueDropdownItem named ? named : new ValueDropdownItem(TextOf(item), item));

            var choices = new List<string>(options.Count);
            foreach (ValueDropdownItem option in options) choices.Add(option.Text);
            dropdown.choices = choices;
        }

        public void Select(int index)
        {
            SerializedProperty property = Property.SerializedProperty;
            SerializedValues.Write(property, options[index].Value);
            property.serializedObject.ApplyModifiedProperties();
            Refresh();
        }

        public void Refresh()
        {
            if (dropdown == null) return;
            dropdown.showMixedValue = Property.SerializedProperty.hasMultipleDifferentValues;
            object current = Property.GetValue();
            for (int i = 0; i < options.Count; i++)
            {
                if (!SerializedValues.AreEqual(options[i].Value, current)) continue;
                dropdown.SetValueWithoutNotify(options[i].Text);
                return;
            }
            dropdown.SetValueWithoutNotify(TextOf(current));
        }

        static string TextOf(object value) => value switch
        {
            null => "None",
            UnityEngine.Object unityObject => unityObject.name,
            string text => text,
            _ => ObjectNames.NicifyVariableName(System.Convert.ToString(value, CultureInfo.InvariantCulture))
        };
    }
}
