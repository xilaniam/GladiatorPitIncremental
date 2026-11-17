using System;
using System.Globalization;
using UnityEditor;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(ProgressBarAttribute))]
    public sealed class ProgressBarHandler : FieldHandler<ProgressBarAttribute>, IRefreshable
    {
        BoundGetter<double> minMember;
        BoundGetter<double> maxMember;
        ProgressBar bar;
        bool dragging;

        public bool NeedsPolling => (Attribute.MinMember != null && !minMember.ReadsSerializedField) || (Attribute.MaxMember != null && !maxMember.ReadsSerializedField);
        public ProgressBar Bar => bar;

        protected override void OnInitialize()
        {
            if (!SerializedValues.IsNumber(Property.SerializedProperty))
            {
                ReportProblem("ProgressBar: only works on numbers");
                return;
            }
            if (Attribute.MinMember != null)
            {
                minMember = ResolveValue<double>(Attribute.MinMember);
                if (!minMember.IsValid) ReportProblem($"ProgressBar: {minMember.Error}");
            }
            if (Attribute.MaxMember != null)
            {
                maxMember = ResolveValue<double>(Attribute.MaxMember);
                if (!maxMember.IsValid) ReportProblem($"ProgressBar: {maxMember.Error}");
            }
        }

        double Min => Attribute.MinMember != null ? minMember.Get() : Attribute.Min;
        double Max => Attribute.MaxMember != null ? maxMember.Get() : Attribute.Max;

        public override VisualElement CreateField()
        {
            if (Problems.Count > 0) return null;
            bar = new ProgressBar();
            bar.AddToClassList("sm-progress-bar");
            bar.RegisterCallback<PointerDownEvent>(down =>
            {
                dragging = true;
                bar.CapturePointer(down.pointerId);
                SetFromPosition(down.localPosition.x);
            });
            bar.RegisterCallback<PointerMoveEvent>(move =>
            {
                if (dragging) SetFromPosition(move.localPosition.x);
            });
            bar.RegisterCallback<PointerUpEvent>(up =>
            {
                dragging = false;
                bar.ReleasePointer(up.pointerId);
            });
            var row = new FieldRow(Property.Label, bar) { name = "sm-progress:" + Property.Path };
            Refresh();
            return row;
        }

        void SetFromPosition(float x)
        {
            float width = bar.resolvedStyle.width;
            if (width > 0f) SetFromRatio(x / width);
        }

        public void SetFromRatio(double ratio)
        {
            double min = Min;
            double value = min + Math.Clamp(ratio, 0.0, 1.0) * (Max - min);
            SerializedProperty property = Property.SerializedProperty;
            SerializedValues.WriteNumber(property, value);
            property.serializedObject.ApplyModifiedProperties();
            Refresh();
        }

        public void Refresh()
        {
            if (bar == null) return;
            double value = SerializedValues.ReadNumber(Property.SerializedProperty);
            bar.lowValue = (float)Min;
            bar.highValue = (float)Max;
            bar.value = (float)value;
            bar.title = Property.SerializedProperty.propertyType == SerializedPropertyType.Integer
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
