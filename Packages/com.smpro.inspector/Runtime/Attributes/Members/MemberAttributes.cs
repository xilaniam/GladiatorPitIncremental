using System;

namespace SMPro.Inspector
{
    public enum ButtonSize
    {
        Small,
        Medium,
        Large
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public sealed class ButtonAttribute : InspectorAttribute
    {
        public ButtonAttribute(string label = null, ButtonSize size = ButtonSize.Small)
        {
            Label = label;
            Size = size;
        }

        public ButtonAttribute(ButtonSize size) : this(null, size)
        {
        }

        public string Label { get; }
        public ButtonSize Size { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class ShowInInspectorAttribute : InspectorAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class OnValueChangedAttribute : InspectorAttribute
    {
        public OnValueChangedAttribute(string method) => Method = method;

        public string Method { get; }
    }
}
