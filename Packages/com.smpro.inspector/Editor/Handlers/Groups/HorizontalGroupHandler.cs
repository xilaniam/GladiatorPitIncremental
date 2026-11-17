using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(HorizontalGroupAttribute))]
    public sealed class HorizontalGroupHandler : GroupHandler<HorizontalGroupAttribute>
    {
        const string AlignedField = "unity-base-field__aligned";

        VisualElement row;

        public override VisualElement CreateGroup()
        {
            row = new VisualElement();
            row.AddToClassList("sm-horizontal");
            return row;
        }

        public override VisualElement ContentFor(GroupAttribute child)
        {
            var cell = new VisualElement();
            cell.AddToClassList("sm-horizontal__cell");
            float width = child is HorizontalGroupAttribute horizontal ? horizontal.Width : 0f;
            if (width <= 0f)
            {
                cell.style.flexGrow = 1;
                cell.style.flexBasis = 0;
            }
            else
            {
                cell.style.width = width <= 1f ? Length.Percent(width * 100f) : new Length(width);
                cell.style.flexShrink = 0;
            }
            cell.RegisterCallback<GeometryChangedEvent>(_ => KeepLabelsCompact(cell));
            row.Add(cell);
            return cell;
        }

        static void KeepLabelsCompact(VisualElement cell)
        {
            cell.Query(className: AlignedField).ForEach(field =>
            {
                field.RemoveFromClassList(AlignedField);
                Label label = field.Q<Label>(className: BaseField<int>.labelUssClassName);
                if (label == null) return;
                label.style.width = StyleKeyword.Null;
                label.style.minWidth = StyleKeyword.Null;
            });
        }
    }
}
