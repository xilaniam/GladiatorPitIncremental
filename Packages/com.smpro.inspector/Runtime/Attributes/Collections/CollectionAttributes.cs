using System;

namespace SMPro.Inspector
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class ListSettingsAttribute : InspectorAttribute
    {
        public bool Draggable { get; set; } = true;
        public bool ShowAddRemove { get; set; } = true;
        public bool ShowFoldout { get; set; } = true;
        public bool IsReadOnly { get; set; }
        public string ElementLabel { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class TableListAttribute : InspectorAttribute
    {
        public bool ShowIndex { get; set; } = true;
        public bool IsReadOnly { get; set; }
    }
}
