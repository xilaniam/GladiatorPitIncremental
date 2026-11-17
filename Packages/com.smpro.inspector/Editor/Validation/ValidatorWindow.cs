using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    public sealed class ValidatorWindow : EditorWindow
    {
        [SerializeField] ValidationOptions options = new();
        [SerializeField] bool showErrors = true;
        [SerializeField] bool showWarnings = true;
        [SerializeField] bool showInfo = true;

        readonly List<ValidationRecord> visible = new();
        ValidationResult result;
        ListView list;
        Label summary;

        public ValidationResult Result => result;
        public IReadOnlyList<ValidationRecord> Visible => visible;
        public ValidationOptions Options => options;

        [MenuItem("Window/SMPro/Validator")]
        public static ValidatorWindow Open()
        {
            var window = GetWindow<ValidatorWindow>();
            window.titleContent = new GUIContent("SMPro Validator");
            return window;
        }

        void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;

            var scopes = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            scopes.Add(Scope("Open scenes", options.includeOpenScenes, value => options.includeOpenScenes = value));
            scopes.Add(Scope("Build scenes", options.includeBuildScenes, value => options.includeBuildScenes = value));
            scopes.Add(Scope("Prefabs", options.includePrefabs, value => options.includePrefabs = value));
            scopes.Add(Scope("ScriptableObjects", options.includeScriptableObjects, value => options.includeScriptableObjects = value));
            root.Add(scopes);

            var folder = new TextField("Folders") { value = string.Join(", ", options.folders), tooltip = "Comma separated folders to search for prefabs and assets" };
            folder.RegisterValueChangedCallback(change => options.folders = SplitFolders(change.newValue));
            root.Add(folder);

            var run = new Button(Run) { text = "Validate", name = "sm-validate" };
            run.style.height = 24;
            root.Add(run);

            var filters = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            filters.Add(Scope("Errors", showErrors, value => { showErrors = value; Filter(); }));
            filters.Add(Scope("Warnings", showWarnings, value => { showWarnings = value; Filter(); }));
            filters.Add(Scope("Info", showInfo, value => { showInfo = value; Filter(); }));
            root.Add(filters);

            summary = new Label("Not run yet") { name = "sm-validation-summary" };
            summary.style.marginTop = 4;
            summary.style.marginBottom = 4;
            root.Add(summary);

            list = new ListView
            {
                name = "sm-validation-results",
                itemsSource = visible,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Single,
                makeItem = () => new HelpBox(),
                bindItem = (element, index) =>
                {
                    var box = (HelpBox)element;
                    ValidationRecord record = visible[index];
                    box.messageType = record.Severity switch
                    {
                        ValidationSeverity.Error => HelpBoxMessageType.Error,
                        ValidationSeverity.Warning => HelpBoxMessageType.Warning,
                        _ => HelpBoxMessageType.Info
                    };
                    box.text = string.IsNullOrEmpty(record.PropertyPath)
                        ? $"{record.Message}\n{record.Location}"
                        : $"{record.Message}\n{record.Location} > {record.PropertyPath}";
                }
            };
            list.style.flexGrow = 1;
            list.selectionChanged += _ =>
            {
                if (list.selectedIndex >= 0 && list.selectedIndex < visible.Count) Reveal(visible[list.selectedIndex]);
            };
            root.Add(list);
        }

        static Toggle Scope(string label, bool value, System.Action<bool> changed)
        {
            var toggle = new Toggle(label) { value = value };
            toggle.style.marginRight = 8;
            toggle.RegisterValueChangedCallback(change => changed(change.newValue));
            return toggle;
        }

        static string[] SplitFolders(string text)
        {
            var folders = new List<string>();
            foreach (string part in text.Split(','))
                if (!string.IsNullOrWhiteSpace(part)) folders.Add(part.Trim().TrimEnd('/'));
            return folders.Count > 0 ? folders.ToArray() : new[] { "Assets" };
        }

        public void Run()
        {
            result = SMValidator.ValidateProject(options);
            Filter();
        }

        void Filter()
        {
            visible.Clear();
            if (result != null)
                foreach (ValidationRecord record in result.Records)
                    if (Shows(record.Severity)) visible.Add(record);
            if (summary != null && result != null)
                summary.text = $"Checked {result.ObjectsChecked} objects: {result.ErrorCount} errors, {result.WarningCount} warnings, {result.Count(ValidationSeverity.Info)} info";
            list?.RefreshItems();
        }

        bool Shows(ValidationSeverity severity) => severity switch
        {
            ValidationSeverity.Error => showErrors,
            ValidationSeverity.Warning => showWarnings,
            _ => showInfo
        };

        public static void Reveal(ValidationRecord record)
        {
            if (record.Context == null) return;
            Selection.activeObject = record.Context;
            EditorGUIUtility.PingObject(record.Context);
        }
    }
}
