using System;

namespace SMPro.Inspector
{
    public readonly struct ValueDropdownItem
    {
        public ValueDropdownItem(string text, object value)
        {
            Text = text;
            Value = value;
        }

        public string Text { get; }
        public object Value { get; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class ValueDropdownAttribute : InspectorAttribute
    {
        public ValueDropdownAttribute(string member) => Member = member;

        public string Member { get; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class EnumToggleButtonsAttribute : InspectorAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class ProgressBarAttribute : InspectorAttribute
    {
        public ProgressBarAttribute(double min = 0, double max = 100)
        {
            Min = min;
            Max = max;
        }

        public double Min { get; }
        public double Max { get; }
        public string MinMember { get; set; }
        public string MaxMember { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class MinMaxSliderAttribute : InspectorAttribute
    {
        public MinMaxSliderAttribute(float min = 0f, float max = 1f)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }
        public string MinMember { get; set; }
        public string MaxMember { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class PreviewFieldAttribute : InspectorAttribute
    {
        public PreviewFieldAttribute(float height = 64f) => Height = height;

        public float Height { get; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class FilePathAttribute : InspectorAttribute
    {
        public string Extensions { get; set; }
        public bool AbsolutePath { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class FolderPathAttribute : InspectorAttribute
    {
        public bool AbsolutePath { get; set; }
    }
}
