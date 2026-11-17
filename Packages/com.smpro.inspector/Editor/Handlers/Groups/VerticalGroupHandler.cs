using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(VerticalGroupAttribute))]
    public sealed class VerticalGroupHandler : GroupHandler<VerticalGroupAttribute>
    {
        VisualElement column;

        public override VisualElement CreateGroup()
        {
            column = new VisualElement();
            column.AddToClassList("sm-vertical");
            return column;
        }

        public override VisualElement ContentFor(GroupAttribute child) => column;
    }
}
