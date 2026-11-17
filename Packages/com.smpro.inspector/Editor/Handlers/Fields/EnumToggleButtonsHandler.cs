using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(EnumToggleButtonsAttribute))]
    public sealed class EnumToggleButtonsHandler : FieldHandler<EnumToggleButtonsAttribute>, IRefreshable
    {
        public const string SelectedClass = "sm-enum-button--selected";

        readonly List<(long value, Button button)> buttons = new();
        bool flags;

        public bool NeedsPolling => false;

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty?.propertyType != SerializedPropertyType.Enum || Property.Member?.ValueType?.IsEnum != true)
                ReportProblem("EnumToggleButtons: only works on enum fields");
        }

        public override VisualElement CreateField()
        {
            if (Problems.Count > 0) return null;
            Type type = Property.Member.ValueType;
            flags = type.IsDefined(typeof(FlagsAttribute), false);

            var strip = new VisualElement();
            strip.AddToClassList("sm-enum-buttons");
            var seen = new HashSet<long>();
            foreach (object value in Enum.GetValues(type))
            {
                long number = Convert.ToInt64(value);
                if ((flags && number == 0) || !seen.Add(number)) continue;
                string name = Enum.GetName(type, value);
                var button = new Button(() => Click(number)) { text = ObjectNames.NicifyVariableName(name), name = "sm-enum:" + name };
                button.AddToClassList("sm-enum-button");
                buttons.Add((number, button));
                strip.Add(button);
            }

            var row = new FieldRow(Property.Label, strip) { name = "sm-enum-buttons:" + Property.Path };
            Refresh();
            return row;
        }

        public void Click(long value)
        {
            SerializedProperty property = Property.SerializedProperty;
            long current = property.intValue;
            long next = !flags ? value : (current & value) == value ? current & ~value : current | value;
            property.intValue = (int)next;
            property.serializedObject.ApplyModifiedProperties();
            Refresh();
        }

        public void Refresh()
        {
            long current = Property.SerializedProperty.intValue;
            foreach ((long value, Button button) in buttons)
            {
                bool selected = flags ? (current & value) == value : current == value;
                button.EnableInClassList(SelectedClass, selected);
            }
        }
    }
}
