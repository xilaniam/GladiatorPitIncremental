using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SMPro.SkillTree.EditorTools
{
    [CustomPropertyDrawer(typeof(StatIdAttribute))]
    public class StatIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject is SkillTreeDefinition tree && property.propertyType == SerializedPropertyType.String)
                IdPopup.Draw(position, property, label, tree.stats.Select(s => s.id).ToList(), tree.stats.Select(s => s.DisplayName).ToList());
            else
                EditorGUI.PropertyField(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(NodeIdAttribute))]
    public class NodeIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject is SkillTreeDefinition tree && property.propertyType == SerializedPropertyType.String)
                IdPopup.Draw(position, property, label, tree.nodes.Select(n => n.id).ToList(), tree.nodes.Select(n => n.DisplayName).ToList());
            else
                EditorGUI.PropertyField(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(StatRef))]
    public class StatRefDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty treeProperty = property.FindPropertyRelative("tree");
            SerializedProperty idProperty = property.FindPropertyRelative("statId");

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float treeWidth = position.width * 0.45f;
            var treeRect = new Rect(position.x, position.y, treeWidth - 2f, position.height);
            var idRect = new Rect(position.x + treeWidth + 2f, position.y, position.width - treeWidth - 2f, position.height);

            EditorGUI.PropertyField(treeRect, treeProperty, GUIContent.none);

            if (treeProperty.objectReferenceValue is SkillTreeDefinition tree)
            {
                IdPopup.Draw(idRect, idProperty, GUIContent.none, tree.stats.Select(s => s.id).ToList(), tree.stats.Select(s => s.DisplayName).ToList());
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.Popup(idRect, 0, new[] { "Pick a tree first" });
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }

    internal static class IdPopup
    {
        public static void Draw(Rect position, SerializedProperty property, GUIContent label, IReadOnlyList<string> ids, IReadOnlyList<string> names)
        {
            string current = property.stringValue;
            var options = new List<GUIContent> { new("(none)") };
            options.AddRange(names.Select(n => new GUIContent(n)));

            int index = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == current) index = i + 1;
            }

            bool missing = index == 0 && !string.IsNullOrEmpty(current);
            if (missing)
            {
                options.Add(new GUIContent($"(missing) {current}"));
                index = options.Count - 1;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int chosen = EditorGUI.Popup(position, label, index, options.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                if (chosen == 0) property.stringValue = "";
                else if (chosen <= ids.Count) property.stringValue = ids[chosen - 1];
            }
            EditorGUI.EndProperty();
        }
    }
}
