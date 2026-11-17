using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    internal interface IGroupHandler
    {
        VisualElement CreateGroup();
        VisualElement ContentFor(GroupAttribute child);
    }

    public abstract class GroupHandler<TAttribute> : AttributeHandler<TAttribute>, IGroupHandler where TAttribute : GroupAttribute
    {
        public abstract VisualElement CreateGroup();
        public abstract VisualElement ContentFor(GroupAttribute child);

        protected string PreferenceKey(string setting) => $"SMInspectorPro/{Property?.OwnerType?.FullName}/{Attribute.Path}/{setting}";
    }
}
