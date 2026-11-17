using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(TitleAttribute))]
    public sealed class TitleHandler : DecoratorHandler<TitleAttribute>
    {
        public override int Order => 20;

        public override VisualElement Decorate(VisualElement field)
        {
            var title = new VisualElement();
            title.AddToClassList("sm-title");

            var text = new Label(Attribute.Title);
            text.AddToClassList("sm-title__text");
            title.Add(text);

            if (!string.IsNullOrEmpty(Attribute.Subtitle))
            {
                var subtitle = new Label(Attribute.Subtitle);
                subtitle.AddToClassList("sm-title__subtitle");
                title.Add(subtitle);
            }

            if (Attribute.HorizontalLine)
            {
                var line = new VisualElement();
                line.AddToClassList("sm-title__line");
                title.Add(line);
            }

            return Stack(title, field);
        }
    }
}
