using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    internal sealed class GroupLayout
    {
        sealed class Node
        {
            readonly IGroupHandler handler;
            readonly VisualElement content;

            public Node(IGroupHandler handler, VisualElement content)
            {
                this.handler = handler;
                this.content = content;
            }

            public VisualElement Place(GroupAttribute child) => handler != null ? handler.ContentFor(child) : content;
        }

        static readonly HashSet<string> reportedConflicts = new();

        readonly Type ownerType;
        readonly Node root;
        readonly Dictionary<string, GroupAttribute> definitions = new(StringComparer.Ordinal);
        readonly Dictionary<string, TabGroupAttribute> pages = new(StringComparer.Ordinal);
        readonly Dictionary<string, Node> nodes = new(StringComparer.Ordinal);

        GroupLayout(VisualElement container, Type ownerType)
        {
            root = new Node(null, container);
            this.ownerType = ownerType;
        }

        public static void Arrange(VisualElement container, Type ownerType, List<InspectorProperty> properties)
        {
            List<InspectorProperty> ordered = Sorted(properties);
            var layout = new GroupLayout(container, ownerType);
            foreach (InspectorProperty property in ordered) layout.Collect(property);
            foreach (InspectorProperty property in ordered) layout.Place(property);
        }

        internal static void ClearReported() => reportedConflicts.Clear();

        static List<InspectorProperty> Sorted(List<InspectorProperty> properties)
        {
            var entries = new List<(int order, int index, InspectorProperty property)>(properties.Count);
            for (int i = 0; i < properties.Count; i++) entries.Add((OrderOf(properties[i]), i, properties[i]));
            entries.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.index.CompareTo(b.index));
            return entries.ConvertAll(entry => entry.property);
        }

        static int OrderOf(InspectorProperty property) => property.Member?.Get<PropertyOrderAttribute>()?.Order ?? 0;

        static string TargetPath(GroupAttribute group) => group is TabGroupAttribute tab ? tab.PagePath : group.Path;

        static int Depth(string path) => path.Split('/').Length;

        void Collect(InspectorProperty property)
        {
            if (property.Member == null) return;
            foreach (InspectorAttribute attribute in property.Member.Attributes)
            {
                if (attribute is not GroupAttribute group || group.Path.Length == 0) continue;
                if (!definitions.TryGetValue(group.Path, out GroupAttribute existing)) definitions[group.Path] = group;
                else if (existing.GetType() != group.GetType()) ReportConflict(group.Path, existing, group);
                if (group is TabGroupAttribute tab && !pages.ContainsKey(tab.PagePath)) pages[tab.PagePath] = tab;
            }
        }

        void Place(InspectorProperty property)
        {
            GroupAttribute target = DeepestGroup(property);
            Node node = target == null ? root : NodeFor(TargetPath(target), property);
            node.Place(target).Add(property.Element);
        }

        static GroupAttribute DeepestGroup(InspectorProperty property)
        {
            if (property.Member == null) return null;
            GroupAttribute deepest = null;
            int deepestDepth = 0;
            foreach (InspectorAttribute attribute in property.Member.Attributes)
            {
                if (attribute is not GroupAttribute group || group.Path.Length == 0) continue;
                int depth = Depth(TargetPath(group));
                if (depth <= deepestDepth) continue;
                deepest = group;
                deepestDepth = depth;
            }
            return deepest;
        }

        Node NodeFor(string path, InspectorProperty firstMember)
        {
            if (path.Length == 0) return root;
            if (nodes.TryGetValue(path, out Node node)) return node;

            if (pages.TryGetValue(path, out TabGroupAttribute tab))
            {
                node = new Node(null, NodeFor(tab.Path, firstMember).Place(tab));
            }
            else
            {
                GroupAttribute definition = definitions.TryGetValue(path, out GroupAttribute declared) ? declared : new VerticalGroupAttribute(path);
                Node parent = NodeFor(definition.ParentPath, firstMember);
                VisualElement element = CreateGroupElement(definition, firstMember, out IGroupHandler handler);
                parent.Place(definition).Add(element);
                node = new Node(handler, element);
            }
            nodes[path] = node;
            return node;
        }

        static VisualElement CreateGroupElement(GroupAttribute definition, InspectorProperty firstMember, out IGroupHandler handler)
        {
            VisualElement element = null;
            try
            {
                handler = HandlerRegistry.Create(firstMember, definition) as IGroupHandler;
                element = handler?.CreateGroup();
            }
            catch (Exception exception)
            {
                handler = null;
                string message = InspectorBuilder.Report(firstMember, HandlerRegistry.HandlerTypeFor(definition.GetType()), exception);
                element = new VisualElement();
                element.Add(new HelpBox(message, HelpBoxMessageType.Error));
            }
            if (element == null)
            {
                handler = null;
                element = new VisualElement();
            }
            if (string.IsNullOrEmpty(element.name)) element.name = "sm-group:" + definition.Path;
            return element;
        }

        void ReportConflict(string path, GroupAttribute used, GroupAttribute ignored)
        {
            if (!reportedConflicts.Add($"{ownerType?.FullName}/{path}")) return;
            Debug.LogWarning($"SMInspectorPro: group '{path}' on {ownerType?.Name} is declared as both {KindOf(used)} and {KindOf(ignored)}; using {KindOf(used)}");
        }

        static string KindOf(GroupAttribute group)
        {
            string name = group.GetType().Name;
            return name.EndsWith("Attribute", StringComparison.Ordinal) ? name.Substring(0, name.Length - "Attribute".Length) : name;
        }
    }
}
