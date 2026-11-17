using System;

namespace SMPro.Inspector
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class TypePickerAttribute : InspectorAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class InlineEditorAttribute : InspectorAttribute
    {
        public InlineEditorAttribute(bool expanded = true) => Expanded = expanded;

        public bool Expanded { get; }
    }
}
