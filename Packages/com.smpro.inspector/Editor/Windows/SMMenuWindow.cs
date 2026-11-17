using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace SMPro.Inspector.EditorTools
{
    public abstract class SMMenuWindow : EditorWindow
    {
        sealed class Node
        {
            public string Name;
            public string Path;
            public MenuEntry Entry;
            public readonly List<Node> Children = new();
        }

        [SerializeField] string selectedPath;
        [SerializeField] float menuWidth = 220f;

        readonly Dictionary<string, int> idsByPath = new();
        readonly List<MenuEntry> matches = new();
        MenuTree tree;
        TreeView menu;
        ListView results;
        VisualElement detail;
        string search = string.Empty;

        public MenuTree Tree => tree;
        public MenuEntry Selected { get; private set; }
        public int VisibleCount => string.IsNullOrEmpty(search) ? tree?.Entries.Count ?? 0 : matches.Count;

        protected abstract void BuildMenu(MenuTree tree);

        protected virtual void CreateGUI()
        {
            rootVisualElement.Clear();
            var split = new TwoPaneSplitView(0, menuWidth, TwoPaneSplitViewOrientation.Horizontal) { name = "sm-menu-split" };

            var left = new VisualElement { name = "sm-menu" };
            var searchField = new ToolbarSearchField { name = "sm-menu-search" };
            searchField.style.width = StyleKeyword.Auto;
            searchField.RegisterValueChangedCallback(change => Search(change.newValue));
            left.Add(searchField);

            menu = new TreeView
            {
                name = "sm-menu-tree",
                makeItem = MakeRow,
                selectionType = SelectionType.Single
            };
            menu.style.flexGrow = 1;
            menu.selectionChanged += selection =>
            {
                foreach (object item in selection)
                    if (item is Node node && node.Entry != null) Show(node.Entry);
            };
            left.Add(menu);

            results = new ListView { name = "sm-menu-results", itemsSource = matches, makeItem = MakeRow, selectionType = SelectionType.Single };
            results.bindItem = (element, index) => BindRow(element, matches[index].Name, matches[index].Icon);
            results.style.flexGrow = 1;
            results.style.display = DisplayStyle.None;
            results.selectionChanged += _ =>
            {
                if (results.selectedIndex >= 0 && results.selectedIndex < matches.Count) Show(matches[results.selectedIndex]);
            };
            left.Add(results);

            detail = new ScrollView { name = "sm-menu-detail" };
            detail.style.flexGrow = 1;
            split.Add(left);
            split.Add(detail);
            rootVisualElement.Add(split);

            RebuildMenu();
        }

        public void RebuildMenu()
        {
            tree = new MenuTree();
            BuildMenu(tree);
            idsByPath.Clear();

            var root = new Node();
            foreach (MenuEntry entry in tree.Entries) Insert(root, entry);
            int nextId = 0;
            List<TreeViewItemData<Node>> items = ToItems(root.Children, ref nextId);
            menu.SetRootItems(items);
            menu.bindItem = (element, index) =>
            {
                var node = menu.GetItemDataForIndex<Node>(index);
                BindRow(element, node.Name, node.Entry?.Icon);
            };
            menu.Rebuild();
            Search(search);
            if (!string.IsNullOrEmpty(selectedPath)) Select(selectedPath);
        }

        static void Insert(Node root, MenuEntry entry)
        {
            string[] parts = entry.Path.Split('/');
            Node current = root;
            for (int i = 0; i < parts.Length; i++)
            {
                string path = string.Join("/", parts, 0, i + 1);
                bool leaf = i == parts.Length - 1;
                Node child = leaf ? null : current.Children.Find(node => node.Entry == null && node.Path == path);
                if (child == null)
                {
                    child = new Node { Name = parts[i], Path = path, Entry = leaf ? entry : null };
                    current.Children.Add(child);
                }
                current = child;
            }
        }

        List<TreeViewItemData<Node>> ToItems(List<Node> nodes, ref int nextId)
        {
            var items = new List<TreeViewItemData<Node>>(nodes.Count);
            foreach (Node node in nodes)
            {
                int id = nextId++;
                if (node.Entry != null && !idsByPath.ContainsKey(node.Path)) idsByPath.Add(node.Path, id);
                items.Add(new TreeViewItemData<Node>(id, node, ToItems(node.Children, ref nextId)));
            }
            return items;
        }

        static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("sm-menu-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.Add(new Image { style = { width = 16, height = 16, marginRight = 4 } });
            row.Add(new Label());
            return row;
        }

        static void BindRow(VisualElement row, string text, Texture icon)
        {
            var image = row.Q<Image>();
            image.image = icon;
            image.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            row.Q<Label>().text = text;
        }

        public void Search(string text)
        {
            search = text?.Trim() ?? string.Empty;
            matches.Clear();
            bool searching = search.Length > 0;
            if (searching && tree != null)
                foreach (MenuEntry entry in tree.Entries)
                    if (entry.Path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) matches.Add(entry);
            if (menu == null) return;
            menu.style.display = searching ? DisplayStyle.None : DisplayStyle.Flex;
            results.style.display = searching ? DisplayStyle.Flex : DisplayStyle.None;
            results.RefreshItems();
        }

        public bool Select(string path)
        {
            if (tree == null) return false;
            foreach (MenuEntry entry in tree.Entries)
            {
                if (entry.Path != path) continue;
                if (idsByPath.TryGetValue(path, out int id)) menu.SetSelectionById(id);
                Show(entry);
                return true;
            }
            return false;
        }

        void Show(MenuEntry entry)
        {
            Selected = entry;
            selectedPath = entry.Path;
            detail.Clear();
            if (entry.Target == null)
            {
                detail.Add(new HelpBox("This entry has no object to show", HelpBoxMessageType.Info));
                return;
            }
            var inspector = new InspectorElement(entry.Target) { name = "sm-menu-inspector" };
            detail.Add(inspector);
            OnSelected(entry);
        }

        protected virtual void OnSelected(MenuEntry entry)
        {
        }
    }
}
