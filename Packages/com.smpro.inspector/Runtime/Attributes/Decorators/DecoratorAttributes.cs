namespace SMPro.Inspector
{
    public enum InfoMessageType
    {
        None,
        Info,
        Warning,
        Error
    }

    public sealed class TitleAttribute : InspectorAttribute
    {
        public TitleAttribute(string title, string subtitle = null, bool horizontalLine = true)
        {
            Title = title;
            Subtitle = subtitle;
            HorizontalLine = horizontalLine;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public bool HorizontalLine { get; }
    }

    public sealed class InfoBoxAttribute : InspectorAttribute
    {
        public InfoBoxAttribute(string message, InfoMessageType type = InfoMessageType.Info, string visibleIf = null)
        {
            Message = message;
            Type = type;
            VisibleIf = visibleIf;
        }

        public string Message { get; }
        public InfoMessageType Type { get; }
        public string VisibleIf { get; }
    }

    public sealed class LabelTextAttribute : InspectorAttribute
    {
        public LabelTextAttribute(string text) => Text = text;

        public string Text { get; }
    }

    public sealed class HideLabelAttribute : InspectorAttribute
    {
    }

    public sealed class SuffixAttribute : InspectorAttribute
    {
        public SuffixAttribute(string text) => Text = text;

        public string Text { get; }
    }

    public sealed class IndentAttribute : InspectorAttribute
    {
        public IndentAttribute(int levels = 1) => Levels = levels;

        public int Levels { get; }
    }
}
