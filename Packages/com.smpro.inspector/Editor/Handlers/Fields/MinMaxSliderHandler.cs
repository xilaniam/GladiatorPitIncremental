using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(MinMaxSliderAttribute))]
    public sealed class MinMaxSliderHandler : FieldHandler<MinMaxSliderAttribute>, IRefreshable
    {
        BoundGetter<float> minMember;
        BoundGetter<float> maxMember;
        MinMaxSlider slider;
        FloatField lowField;
        FloatField highField;

        public bool NeedsPolling => (Attribute.MinMember != null && !minMember.ReadsSerializedField) || (Attribute.MaxMember != null && !maxMember.ReadsSerializedField);
        public MinMaxSlider Slider => slider;

        bool IsInt => Property.SerializedProperty.propertyType == SerializedPropertyType.Vector2Int;
        float MinLimit => Attribute.MinMember != null ? minMember.Get() : Attribute.Min;
        float MaxLimit => Attribute.MaxMember != null ? maxMember.Get() : Attribute.Max;

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty?.propertyType is not (SerializedPropertyType.Vector2 or SerializedPropertyType.Vector2Int))
            {
                ReportProblem("MinMaxSlider: only works on Vector2 and Vector2Int fields");
                return;
            }
            if (Attribute.MinMember != null)
            {
                minMember = ResolveValue<float>(Attribute.MinMember);
                if (!minMember.IsValid) ReportProblem($"MinMaxSlider: {minMember.Error}");
            }
            if (Attribute.MaxMember != null)
            {
                maxMember = ResolveValue<float>(Attribute.MaxMember);
                if (!maxMember.IsValid) ReportProblem($"MinMaxSlider: {maxMember.Error}");
            }
        }

        public override VisualElement CreateField()
        {
            if (Problems.Count > 0) return null;
            slider = new MinMaxSlider(Property.Label, 0f, 1f, MinLimit, MaxLimit);
            slider.AddToClassList(BaseField<Vector2>.alignedFieldUssClassName);
            slider.style.flexGrow = 1;
            slider.RegisterValueChangedCallback(change =>
            {
                if (change.target == slider) Set(change.newValue);
            });

            lowField = NumberField();
            lowField.RegisterValueChangedCallback(change => Set(new Vector2(change.newValue, Current.y)));
            highField = NumberField();
            highField.RegisterValueChangedCallback(change => Set(new Vector2(Current.x, change.newValue)));

            var row = new VisualElement { name = "sm-min-max:" + Property.Path };
            row.AddToClassList("sm-min-max");
            row.Add(slider);
            row.Add(lowField);
            row.Add(highField);
            Refresh();
            return row;
        }

        static FloatField NumberField()
        {
            var field = new FloatField();
            field.AddToClassList("sm-min-max__number");
            return field;
        }

        Vector2 Current
        {
            get
            {
                SerializedProperty property = Property.SerializedProperty;
                return IsInt ? (Vector2)property.vector2IntValue : property.vector2Value;
            }
        }

        public void Set(Vector2 value)
        {
            float low = Mathf.Clamp(Mathf.Min(value.x, value.y), MinLimit, MaxLimit);
            float high = Mathf.Clamp(Mathf.Max(value.x, value.y), MinLimit, MaxLimit);
            SerializedProperty property = Property.SerializedProperty;
            if (IsInt) property.vector2IntValue = new Vector2Int(Mathf.RoundToInt(low), Mathf.RoundToInt(high));
            else property.vector2Value = new Vector2(low, high);
            property.serializedObject.ApplyModifiedProperties();
            Refresh();
        }

        public void Refresh()
        {
            if (slider == null) return;
            Vector2 value = Current;
            slider.lowLimit = MinLimit;
            slider.highLimit = MaxLimit;
            slider.SetValueWithoutNotify(value);
            lowField.SetValueWithoutNotify(value.x);
            highField.SetValueWithoutNotify(value.y);
        }
    }
}
