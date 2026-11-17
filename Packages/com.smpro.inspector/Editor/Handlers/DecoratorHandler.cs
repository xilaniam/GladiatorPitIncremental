using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    internal interface IDecoratorHandler
    {
        int Order { get; }
        VisualElement Decorate(VisualElement field);
    }

    public abstract class DecoratorHandler<TAttribute> : AttributeHandler<TAttribute>, IDecoratorHandler where TAttribute : InspectorAttribute
    {
        public virtual int Order => 0;

        public abstract VisualElement Decorate(VisualElement field);

        protected static VisualElement Stack(params VisualElement[] parts)
        {
            var container = new VisualElement();
            foreach (VisualElement part in parts) container.Add(part);
            return container;
        }
    }
}
