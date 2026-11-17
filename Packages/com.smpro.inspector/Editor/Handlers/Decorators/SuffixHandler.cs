using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(SuffixAttribute))]
    public sealed class SuffixHandler : DecoratorHandler<SuffixAttribute>
    {
        public override VisualElement Decorate(VisualElement field)
        {
            var row = new VisualElement();
            row.AddToClassList("sm-suffix");
            field.style.flexGrow = 1;
            row.Add(field);

            var suffix = new Label(Attribute.Text);
            suffix.AddToClassList("sm-suffix__label");
            row.Add(suffix);
            return row;
        }
    }
}
