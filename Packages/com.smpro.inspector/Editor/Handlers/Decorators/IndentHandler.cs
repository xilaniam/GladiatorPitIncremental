using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(IndentAttribute))]
    public sealed class IndentHandler : DecoratorHandler<IndentAttribute>
    {
        public const float StepWidth = 15f;

        public override int Order => 30;

        public override VisualElement Decorate(VisualElement field)
        {
            field.style.marginLeft = StepWidth * Attribute.Levels;
            return field;
        }
    }
}
