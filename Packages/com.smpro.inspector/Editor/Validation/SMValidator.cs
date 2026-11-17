using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.EditorTools
{
    [Serializable]
    public sealed class ValidationOptions
    {
        public bool includeOpenScenes = true;
        public bool includeBuildScenes = true;
        public bool includePrefabs = true;
        public bool includeScriptableObjects = true;
        public string[] folders = { "Assets" };

        public static ValidationOptions ForBuild => new() { includeOpenScenes = false };
    }

    public readonly struct ValidationRecord
    {
        public ValidationRecord(ValidationIssue issue, string location)
        {
            Severity = issue.Severity;
            Message = issue.Message;
            PropertyPath = issue.PropertyPath;
            Context = issue.Context;
            Location = location;
        }

        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public string PropertyPath { get; }
        public Object Context { get; }
        public string Location { get; }

        public override string ToString() =>
            string.IsNullOrEmpty(PropertyPath) ? $"[{Severity}] {Location}: {Message}" : $"[{Severity}] {Location} ({PropertyPath}): {Message}";
    }

    public sealed class ValidationResult
    {
        readonly List<ValidationRecord> records = new();

        public IReadOnlyList<ValidationRecord> Records => records;
        public int ObjectsChecked { get; internal set; }
        public int ErrorCount => Count(ValidationSeverity.Error);
        public int WarningCount => Count(ValidationSeverity.Warning);

        public int Count(ValidationSeverity severity)
        {
            int count = 0;
            foreach (ValidationRecord record in records)
                if (record.Severity == severity) count++;
            return count;
        }

        internal void Add(ValidationRecord record) => records.Add(record);
    }

    public static class SMValidator
    {
        public static ValidationReport Validate(Object target, ValidationReport report = null)
        {
            report ??= new ValidationReport();
            switch (target)
            {
                case null:
                    return report;
                case GameObject gameObject:
                    ValidateHierarchy(gameObject, report);
                    return report;
            }

            report.Context = target;
            report.PropertyPath = null;
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(target))
                report.Error("Has [SerializeReference] data whose class no longer exists");
            if (!TypeModel.For(target.GetType()).UsesInspectorAttributes) return report;

            using var serializedObject = new SerializedObject(target);
            InspectorBuilder.Result built = InspectorBuilder.BuildContent(new VisualElement(), serializedObject, () => { });
            built.Refresher.Validation.Run(report);

            report.Context = target;
            report.PropertyPath = null;
            foreach (InspectorProperty property in built.Refresher.Properties)
            {
                foreach (AttributeHandler handler in property.Handlers)
                    foreach (string problem in handler.Problems)
                        report.Error(problem, property.Path);
                foreach (string problem in property.BuilderProblems) report.Error(problem, property.Path);
            }
            return report;
        }

        static void ValidateHierarchy(GameObject root, ValidationReport report)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject gameObject = transform.gameObject;
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                if (missing > 0)
                {
                    report.Context = gameObject;
                    report.PropertyPath = null;
                    report.Error(missing == 1 ? "Has a missing script" : $"Has {missing} missing scripts");
                }
                foreach (Component component in gameObject.GetComponents<Component>())
                    if (component is MonoBehaviour) Validate(component, report);
            }
        }

        public static ValidationResult ValidateProject(ValidationOptions options)
        {
            options ??= new ValidationOptions();
            var result = new ValidationResult();
            var report = new ValidationReport();
            try
            {
                if (options.includeOpenScenes)
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        ValidateScene(SceneManager.GetSceneAt(i), report, result);
                if (options.includeBuildScenes) ValidateBuildScenes(options, report, result);
                if (options.includePrefabs) ValidatePrefabs(options, report, result);
                if (options.includeScriptableObjects) ValidateScriptableObjects(options, report, result);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            return result;
        }

        public static void ValidateScene(Scene scene, ValidationReport report, ValidationResult result)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ValidateHierarchy(root, report);
                result.ObjectsChecked++;
                Collect(report, result, context => $"{SceneName(scene)}: {HierarchyPath(context)}");
            }
        }

        static void ValidateBuildScenes(ValidationOptions options, ValidationReport report, ValidationResult result)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!scenes[i].enabled) continue;
                Scene loaded = SceneManager.GetSceneByPath(scenes[i].path);
                if (loaded.IsValid() && loaded.isLoaded)
                {
                    if (!options.includeOpenScenes) ValidateScene(loaded, report, result);
                    continue;
                }
                Progress("Scenes", scenes[i].path, i, scenes.Length);
                Scene opened = EditorSceneManager.OpenScene(scenes[i].path, OpenSceneMode.Additive);
                try
                {
                    ValidateScene(opened, report, result);
                }
                finally
                {
                    EditorSceneManager.CloseScene(opened, true);
                }
            }
        }

        static void ValidatePrefabs(ValidationOptions options, ValidationReport report, ValidationResult result)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", options.folders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Progress("Prefabs", path, i, guids.Length);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                ValidateHierarchy(prefab, report);
                result.ObjectsChecked++;
                Collect(report, result, context => $"{path}: {HierarchyPath(context)}");
            }
        }

        static void ValidateScriptableObjects(ValidationOptions options, ValidationReport report, ValidationResult result)
        {
            var seen = new HashSet<string>();
            List<Type> types = RuleTypes();
            foreach (Type type in types)
            {
                string[] guids = AssetDatabase.FindAssets("t:" + type.Name, options.folders);
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!seen.Add(path)) continue;
                    Progress("Assets", path, i, guids.Length);
                    foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (asset is not ScriptableObject || !type.IsInstanceOfType(asset)) continue;
                        Validate(asset, report);
                        result.ObjectsChecked++;
                        Collect(report, result, _ => path);
                    }
                }
            }
        }

        static List<Type> RuleTypes()
        {
            var types = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
                if (!type.IsAbstract && !type.IsGenericType && TypeModel.For(type).UsesInspectorAttributes) types.Add(type);
            return types;
        }

        static void Collect(ValidationReport report, ValidationResult result, Func<Object, string> location)
        {
            foreach (ValidationIssue issue in report.Issues) result.Add(new ValidationRecord(issue, location(issue.Context)));
            report.Clear();
        }

        static void Progress(string kind, string path, int index, int count)
        {
            if (Application.isBatchMode) return;
            EditorUtility.DisplayProgressBar("SMInspectorPro validation", $"{kind}: {path}", count == 0 ? 1f : (float)index / count);
        }

        static string SceneName(Scene scene) => string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;

        static string HierarchyPath(Object context)
        {
            Transform transform = context switch
            {
                Component component => component.transform,
                GameObject gameObject => gameObject.transform,
                _ => null
            };
            if (transform == null) return context != null ? context.name : "?";
            var path = new StringBuilder(transform.name);
            for (Transform parent = transform.parent; parent != null; parent = parent.parent) path.Insert(0, parent.name + "/");
            return context is Component ? $"{path} ({context.GetType().Name})" : path.ToString();
        }

        public static void RunFromCommandLine()
        {
            ValidationResult result = ValidateProject(ValidationOptions.ForBuild);
            foreach (ValidationRecord record in result.Records) Log(record);
            Debug.Log($"SMInspectorPro: checked {result.ObjectsChecked} objects, {result.ErrorCount} errors, {result.WarningCount} warnings");
            EditorApplication.Exit(result.ErrorCount > 0 ? 1 : 0);
        }

        internal static void Log(ValidationRecord record)
        {
            switch (record.Severity)
            {
                case ValidationSeverity.Error:
                    Debug.LogError(record.ToString(), record.Context);
                    break;
                case ValidationSeverity.Warning:
                    Debug.LogWarning(record.ToString(), record.Context);
                    break;
                default:
                    Debug.Log(record.ToString(), record.Context);
                    break;
            }
        }
    }
}
