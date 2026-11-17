using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(InfoBoxAttribute))]
    public sealed class InfoBoxHandler : DecoratorHandler<InfoBoxAttribute>
    {
        public override int Order => 10;

        public HelpBox Box { get; private set; }

        public override VisualElement Decorate(VisualElement field)
        {
            Box = new HelpBox(Attribute.Message, ToHelpBoxType(Attribute.Type));
            Box.AddToClassList("sm-info-box");
            return Stack(Box, field);
        }

        internal static HelpBoxMessageType ToHelpBoxType(InfoMessageType type) => type switch
        {
            InfoMessageType.Info => HelpBoxMessageType.Info,
            InfoMessageType.Warning => HelpBoxMessageType.Warning,
            InfoMessageType.Error => HelpBoxMessageType.Error,
            _ => HelpBoxMessageType.None
        };
    }
}
