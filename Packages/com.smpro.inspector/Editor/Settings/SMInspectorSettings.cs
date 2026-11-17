using System;
using System.Collections.Generic;
using UnityEditor;

namespace SMPro.Inspector.EditorTools
{
    [UnityEditor.FilePath("ProjectSettings/SMInspectorSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
    public sealed class SMInspectorSettings : ScriptableSingleton<SMInspectorSettings>
    {
        public const int MinimumPollInterval = 16;

        public bool useFallbackInspector = true;
        public bool validateBeforeBuild;
        public bool failBuildOnErrors = true;
        public int pollInterval = 100;
        public List<string> skippedNamespaces = new();

        public long PollInterval => Math.Max(MinimumPollInterval, pollInterval);

        public bool IsSkipped(Type type)
        {
            string space = type?.Namespace;
            if (string.IsNullOrEmpty(space)) return false;
            foreach (string skipped in skippedNamespaces)
            {
                if (string.IsNullOrWhiteSpace(skipped)) continue;
                string prefix = skipped.Trim();
                if (space == prefix || space.StartsWith(prefix + ".", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public bool AllowsFallbackFor(Type type) => useFallbackInspector && !IsSkipped(type);

        public void Save() => Save(true);
    }
}
