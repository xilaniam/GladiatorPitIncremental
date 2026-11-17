using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    internal static class SMInspectorSettingsProvider
    {
        public const string Path = "Project/SMPro/Inspector";

        [SettingsProvider]
        public static SettingsProvider Create() => new(Path, SettingsScope.Project)
        {
            label = "Inspector",
            keywords = new HashSet<string> { "SMInspectorPro", "inspector", "validation", "fallback", "attributes" },
            activateHandler = (_, root) => Build(root)
        };

        internal static void Build(VisualElement root)
        {
            SMInspectorSettings settings = SMInspectorSettings.instance;
            settings.hideFlags &= ~UnityEngine.HideFlags.NotEditable;
            var serializedObject = new SerializedObject(settings);

            var page = new VisualElement { name = "sm-settings" };
            page.style.paddingLeft = 10;
            page.style.paddingTop = 4;
            page.Add(new Label("SMInspectorPro") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold, fontSize = 18, marginBottom = 6 } });

            page.Add(Field(serializedObject, nameof(SMInspectorSettings.useFallbackInspector), "Use the inspector for attributed types",
                "Draw MonoBehaviours and ScriptableObjects that use SMInspectorPro attributes with the package. Custom editors deriving from SMInspectorEditor are not affected."));
            page.Add(Field(serializedObject, nameof(SMInspectorSettings.skippedNamespaces), "Namespaces to skip",
                "Types in these namespaces, and namespaces below them, keep Unity's default inspector."));
            page.Add(Field(serializedObject, nameof(SMInspectorSettings.pollInterval), "Poll interval (ms)",
                $"How often conditions that read properties or methods are re-checked while an inspector is open. Minimum {SMInspectorSettings.MinimumPollInterval} ms."));
            page.Add(Field(serializedObject, nameof(SMInspectorSettings.validateBeforeBuild), "Validate before build",
                "Run the project validator over build scenes, prefabs and ScriptableObjects before every build."));
            page.Add(Field(serializedObject, nameof(SMInspectorSettings.failBuildOnErrors), "Fail the build on errors",
                "Stop the build when validation finds errors. Warnings never stop it."));
            page.Add(new Button(() => ValidatorWindow.Open()) { text = "Open Validator", name = "sm-open-validator", style = { alignSelf = Align.FlexStart, marginTop = 8 } });

            page.TrackSerializedObjectValue(serializedObject, _ =>
            {
                settings.Save();
                ActiveEditorTracker.sharedTracker.ForceRebuild();
            });
            page.Bind(serializedObject);
            root.Add(page);
        }

        static PropertyField Field(SerializedObject serializedObject, string name, string label, string tooltip) =>
            new(serializedObject.FindProperty(name), label) { name = "PropertyField:" + name, tooltip = tooltip };
    }
}
