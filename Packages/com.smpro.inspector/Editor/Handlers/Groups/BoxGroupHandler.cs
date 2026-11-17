using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(BoxGroupAttribute))]
    public sealed class BoxGroupHandler : GroupHandler<BoxGroupAttribute>
    {
        VisualElement body;

        public override VisualElement CreateGroup()
        {
            var box = new VisualElement();
            box.AddToClassList("sm-box");
            if (Attribute.ShowLabel)
            {
                var title = new Label(Attribute.Name);
                title.AddToClassList("sm-box__title");
                box.Add(title);
            }
            body = new VisualElement();
            body.AddToClassList("sm-box__body");
            box.Add(body);
            return box;
        }

        public override VisualElement ContentFor(GroupAttribute child) => body;
    }
}
