using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(ShowInInspectorAttribute))]
    public sealed class ShowInInspectorHandler : FieldHandler<ShowInInspectorAttribute>, IRefreshable
    {
        IReadOnlyDisplay display;

        public bool NeedsPolling => display != null;

        public override VisualElement CreateField()
        {
            MemberModel member = Property.Member;
            if (member == null || member.Kind == MemberKind.SerializedField || member.Kind == MemberKind.Method) return null;

            Type fieldType = ValueFields.FieldValueType(member.ValueType);
            display = fieldType != null
                ? (IReadOnlyDisplay)Activator.CreateInstance(typeof(ReadOnlyDisplay<>).MakeGenericType(fieldType), Property)
                : new TextDisplay(Property);

            var container = new VisualElement { name = "sm-member:" + Property.Path };
            container.AddToClassList("sm-read-only");
            container.Add(display.Element);
            display.Element.SetEnabled(false);
            try
            {
                display.Refresh();
            }
            catch (Exception exception)
            {
                display.Element.tooltip = exception.Message;
            }
            return container;
        }

        public void Refresh() => display?.Refresh();

        interface IReadOnlyDisplay
        {
            VisualElement Element { get; }
            void Refresh();
        }

        sealed class ReadOnlyDisplay<T> : IReadOnlyDisplay
        {
            readonly BaseField<T> field;
            readonly BoundGetter<T> getter;
            bool hasValue;
            T shown;

            public ReadOnlyDisplay(InspectorProperty property)
            {
                field = ((ValueField<T>)ValueFields.Create(property.Member.ValueType, property.Label)).Field;
                getter = BoundGetter<T>.Value(property, property.Member.Info.Name);
            }

            public VisualElement Element => field;

            public void Refresh()
            {
                if (!getter.IsValid) return;
                T value = getter.Get();
                if (hasValue && EqualityComparer<T>.Default.Equals(value, shown)) return;
                hasValue = true;
                shown = value;
                field.SetValueWithoutNotify(value);
            }
        }

        sealed class TextDisplay : IReadOnlyDisplay
        {
            readonly TextField field;
            readonly BoundGetter<object> getter;
            object shown;
            bool hasValue;

            public TextDisplay(InspectorProperty property)
            {
                field = new TextField(property.Label);
                getter = BoundGetter<object>.Value(property, property.Member.Info.Name);
            }

            public VisualElement Element => field;

            public void Refresh()
            {
                if (!getter.IsValid) return;
                object value = getter.Get();
                if (hasValue && ReferenceEquals(value, shown)) return;
                hasValue = true;
                shown = value;
                field.SetValueWithoutNotify(value?.ToString() ?? "null");
            }
        }
    }
}
