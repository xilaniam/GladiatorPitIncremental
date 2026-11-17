using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SMPro.Inspector.EditorTools
{
    public sealed class MenuEntry
    {
        internal MenuEntry(string path, Object target, Texture icon)
        {
            Path = path;
            Target = target;
            Icon = icon;
            int slash = path.LastIndexOf('/');
            Name = slash < 0 ? path : path.Substring(slash + 1);
        }

        public string Path { get; }
        public string Name { get; }
        public Object Target { get; }
        public Texture Icon { get; }
    }

    public sealed class MenuTree
    {
        readonly List<MenuEntry> entries = new();

        public IReadOnlyList<MenuEntry> Entries => entries;

        public MenuEntry Add(string path, Object target, Texture icon = null)
        {
            string normalized = Normalize(path);
            if (normalized.Length == 0) return null;
            var entry = new MenuEntry(normalized, target, icon != null ? icon : target != null ? AssetPreview.GetMiniThumbnail(target) : null);
            entries.Add(entry);
            return entry;
        }

        public void AddAllAssetsOf<T>(string menuFolder, params string[] searchFolders) where T : Object
        {
            string[] folders = searchFolders != null && searchFolders.Length > 0 ? searchFolders : new[] { "Assets" };
            var found = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, folders))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                    if (asset is T typed && !found.Contains(typed)) found.Add(typed);
            }
            found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            string prefix = Normalize(menuFolder);
            foreach (T asset in found) Add(prefix.Length == 0 ? asset.name : prefix + "/" + asset.name, asset);
        }

        static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            var parts = new List<string>();
            foreach (string part in path.Split('/'))
                if (!string.IsNullOrWhiteSpace(part)) parts.Add(part.Trim());
            return string.Join("/", parts);
        }
    }
}
