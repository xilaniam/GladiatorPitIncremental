using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(TabGroupAttribute))]
    public sealed class TabGroupHandler : GroupHandler<TabGroupAttribute>
    {
        readonly Dictionary<string, Tab> pages = new();
        TabView tabView;
        string key;
        int savedIndex;

        public override VisualElement CreateGroup()
        {
            key = PreferenceKey("tab");
            savedIndex = EditorPrefs.GetInt(key, 0);
            tabView = new TabView();
            tabView.AddToClassList("sm-tab-group");
            tabView.activeTabChanged += (_, _) => EditorPrefs.SetInt(key, tabView.selectedTabIndex);
            return tabView;
        }

        public override VisualElement ContentFor(GroupAttribute child)
        {
            string tab = child is TabGroupAttribute tabbed ? tabbed.Tab : child.Name;
            if (pages.TryGetValue(tab, out Tab page)) return page.contentContainer;

            page = new Tab(tab) { name = "sm-tab:" + tab };
            pages.Add(tab, page);
            tabView.Add(page);
            if (pages.Count - 1 == savedIndex) tabView.activeTab = page;
            return page.contentContainer;
        }
    }
}
