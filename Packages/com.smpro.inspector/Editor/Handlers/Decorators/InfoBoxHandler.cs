using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(InfoBoxAttribute))]
    public sealed class InfoBoxHandler : DecoratorHandler<InfoBoxAttribute>, IRefreshable
    {
        BoundGetter<bool> visibleIf;
        bool conditional;
        bool shown = true;

        public override int Order => 10;

        public HelpBox Box { get; private set; }

        public bool NeedsPolling => conditional && !visibleIf.ReadsSerializedField;

        protected override void OnInitialize()
        {
            if (string.IsNullOrEmpty(Attribute.VisibleIf)) return;
            visibleIf = ResolveCondition(Attribute.VisibleIf, null);
            conditional = visibleIf.IsValid;
            if (!conditional) ReportProblem($"{AttributeName}: {visibleIf.Error}");
        }

        public override VisualElement Decorate(VisualElement field)
        {
            Box = new HelpBox(Attribute.Message, ToHelpBoxType(Attribute.Type));
            Box.AddToClassList("sm-info-box");
            return Stack(Box, field);
        }

        public void Refresh()
        {
            if (!conditional || Box == null) return;
            bool show = visibleIf.Get();
            if (show == shown) return;
            shown = show;
            Box.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
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
