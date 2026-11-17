using System;

namespace SMPro.Inspector
{
    public abstract class GroupAttribute : InspectorAttribute
    {
        protected GroupAttribute(string path)
        {
            Path = Normalize(path);
            int slash = Path.LastIndexOf('/');
            Name = slash < 0 ? Path : Path.Substring(slash + 1);
            ParentPath = slash < 0 ? string.Empty : Path.Substring(0, slash);
        }

        public string Path { get; }
        public string Name { get; }
        public string ParentPath { get; }

        static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++) segments[i] = segments[i].Trim();
            return string.Join("/", Array.FindAll(segments, segment => segment.Length > 0));
        }
    }

    public sealed class BoxGroupAttribute : GroupAttribute
    {
        public BoxGroupAttribute(string path, bool showLabel = true) : base(path) => ShowLabel = showLabel;

        public bool ShowLabel { get; }
    }

    public sealed class FoldoutGroupAttribute : GroupAttribute
    {
        public FoldoutGroupAttribute(string path, bool expanded = false) : base(path) => Expanded = expanded;

        public bool Expanded { get; }
    }

    public sealed class TabGroupAttribute : GroupAttribute
    {
        public TabGroupAttribute(string path, string tab) : base(path)
        {
            Tab = tab?.Trim() ?? string.Empty;
            PagePath = Path + "/" + Tab;
        }

        public string Tab { get; }
        public string PagePath { get; }
    }

    public sealed class HorizontalGroupAttribute : GroupAttribute
    {
        public HorizontalGroupAttribute(string path, float width = 0f) : base(path) => Width = width;

        public float Width { get; }
    }

    public sealed class VerticalGroupAttribute : GroupAttribute
    {
        public VerticalGroupAttribute(string path) : base(path)
        {
        }
    }
}
