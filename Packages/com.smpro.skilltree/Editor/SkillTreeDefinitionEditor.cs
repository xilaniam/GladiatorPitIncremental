using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SMPro.SkillTree.EditorTools
{
    [CustomEditor(typeof(SkillTreeDefinition))]
    public class SkillTreeDefinitionEditor : Editor
    {
        List<ValidationIssue> issues = new();
        bool showWarnings;

        void OnEnable() => Revalidate();

        public override void OnInspectorGUI()
        {
            var tree = (SkillTreeDefinition)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Graph", GUILayout.Height(24))) SkillTreeGraphWindow.Open(tree);
                if (GUILayout.Button("Open Table", GUILayout.Height(24))) SkillTreeTableWindow.Open(tree);
                if (GUILayout.Button("Validate", GUILayout.Height(24))) Revalidate();
            }

            DrawIssues();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                tree.InvalidateLookups();
                Revalidate();
            }
        }

        void DrawIssues()
        {
            List<ValidationIssue> errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
            List<ValidationIssue> warnings = issues.Where(i => i.Severity == IssueSeverity.Warning).ToList();

            if (errors.Count == 0 && warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("No problems found.", MessageType.Info);
                return;
            }

            foreach (ValidationIssue error in errors) EditorGUILayout.HelpBox(error.ToString(), MessageType.Error);

            if (warnings.Count == 0) return;
            showWarnings = EditorGUILayout.Foldout(showWarnings, $"{warnings.Count} warning(s)", true);
            if (showWarnings)
                foreach (ValidationIssue warning in warnings) EditorGUILayout.HelpBox(warning.ToString(), MessageType.Warning);
        }

        void Revalidate()
        {
            if (target is SkillTreeDefinition tree) issues = SkillTreeValidation.Validate(tree);
        }

        [OnOpenAsset]
        static bool OpenGraphOnDoubleClick(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not SkillTreeDefinition tree) return false;
            SkillTreeGraphWindow.Open(tree);
            return true;
        }
    }

    internal class SkillTreeValidationOnImport : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (!path.EndsWith(".asset")) continue;
                var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(path);
                if (tree == null) continue;

                tree.InvalidateLookups();
                foreach (ValidationIssue issue in SkillTreeValidation.Validate(tree).Where(i => i.Severity == IssueSeverity.Error))
                    Debug.LogError($"[SkillTree] {tree.name}: {issue}", tree);
            }
        }
    }
}
