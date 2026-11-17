using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SMPro.SkillTree.EditorTools
{
    public class SkillTreeTableWindow : EditorWindow
    {
        const float NameWidth = 170f;
        const float NumberWidth = 80f;
        const float CurveWidth = 100f;
        const float CurrencyWidth = 90f;
        const float TotalWidth = 120f;

        [SerializeField] SkillTreeDefinition tree;
        [SerializeField] string filter = "";
        Vector2 scroll;
        SerializedObject serializedTree;

        [MenuItem("Window/SMPro/Skill Tree Table")]
        static void OpenEmpty() => GetWindow<SkillTreeTableWindow>("Skill Tree Table");

        public static void Open(SkillTreeDefinition definition)
        {
            var window = GetWindow<SkillTreeTableWindow>("Skill Tree Table");
            window.tree = definition;
            window.serializedTree = null;
            window.Focus();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var picked = (SkillTreeDefinition)EditorGUILayout.ObjectField(tree, typeof(SkillTreeDefinition), false, GUILayout.Width(240));
                if (picked != tree)
                {
                    tree = picked;
                    serializedTree = null;
                }
                filter = EditorGUILayout.TextField(filter, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(tree == null))
                {
                    if (GUILayout.Button("Graph", EditorStyles.toolbarButton)) SkillTreeGraphWindow.Open(tree);
                    if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton)) ExportCsv();
                }
            }

            if (tree == null)
            {
                EditorGUILayout.HelpBox("Pick a Skill Tree Definition.", MessageType.Info);
                return;
            }

            serializedTree ??= new SerializedObject(tree);
            serializedTree.Update();

            DrawHeader();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            SerializedProperty nodes = serializedTree.FindProperty("nodes");
            long grandTotal = 0;
            for (int i = 0; i < nodes.arraySize; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                if (node == null) continue;
                grandTotal = Add(grandTotal, node.GetTotalCost(node.maxLevel));
                if (!string.IsNullOrEmpty(filter) && node.DisplayName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                DrawRow(nodes.GetArrayElementAtIndex(i), node);
            }
            EditorGUILayout.EndScrollView();

            if (serializedTree.ApplyModifiedProperties()) tree.InvalidateLookups();

            EditorGUILayout.LabelField($"{tree.nodes.Count} nodes · {tree.nodes.Where(n => n != null).Sum(n => n.maxLevel)} levels · total cost to max everything: {grandTotal:N0}", EditorStyles.miniBoldLabel);
        }

        static void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(NameWidth));
                GUILayout.Label("Max Level", EditorStyles.boldLabel, GUILayout.Width(NumberWidth));
                GUILayout.Label("Cost Curve", EditorStyles.boldLabel, GUILayout.Width(CurveWidth));
                GUILayout.Label("Base", EditorStyles.boldLabel, GUILayout.Width(NumberWidth));
                GUILayout.Label("Growth/Step", EditorStyles.boldLabel, GUILayout.Width(NumberWidth));
                GUILayout.Label("Currency", EditorStyles.boldLabel, GUILayout.Width(CurrencyWidth));
                GUILayout.Label("Total to Max", EditorStyles.boldLabel, GUILayout.Width(TotalWidth));
            }
        }

        static void DrawRow(SerializedProperty property, SkillNodeDefinition node)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("displayName"), GUIContent.none, GUILayout.Width(NameWidth));
                SerializedProperty maxLevel = property.FindPropertyRelative("maxLevel");
                maxLevel.intValue = Mathf.Max(1, EditorGUILayout.IntField(maxLevel.intValue, GUILayout.Width(NumberWidth)));

                SerializedProperty cost = property.FindPropertyRelative("cost");
                GUILayout.Label(node.cost == null ? "None" : ObjectNames.NicifyVariableName(node.cost.GetType().Name), GUILayout.Width(CurveWidth));
                DrawCostField(cost, node.cost, "baseCost", "amount");
                DrawCostField(cost, node.cost, "growth", "step");

                EditorGUILayout.PropertyField(property.FindPropertyRelative("currencyId"), GUIContent.none, GUILayout.Width(CurrencyWidth));
                GUILayout.Label(node.GetTotalCost(node.maxLevel).ToString("N0"), GUILayout.Width(TotalWidth));
            }
        }

        static void DrawCostField(SerializedProperty cost, CostCurve curve, params string[] names)
        {
            SerializedProperty field = curve == null ? null : names.Select(cost.FindPropertyRelative).FirstOrDefault(p => p != null);
            if (field == null)
            {
                GUILayout.Label("-", GUILayout.Width(NumberWidth));
                return;
            }
            EditorGUILayout.PropertyField(field, GUIContent.none, GUILayout.Width(NumberWidth));
        }

        void ExportCsv()
        {
            string path = EditorUtility.SaveFilePanel("Export Skill Tree", "", tree.name + ".csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var csv = new StringBuilder("id,name,max level,cost curve,currency,costs per level,total to max\n");
            foreach (SkillNodeDefinition node in tree.nodes.Where(n => n != null))
            {
                string costs = string.Join(" ", Enumerable.Range(0, node.maxLevel).Select(node.GetCost));
                string curve = node.cost == null ? "None" : node.cost.GetType().Name;
                csv.AppendLine($"{Quote(node.id)},{Quote(node.DisplayName)},{node.maxLevel},{curve},{Quote(node.currencyId)},{costs},{node.GetTotalCost(node.maxLevel)}");
            }
            File.WriteAllText(path, csv.ToString());
            EditorUtility.RevealInFinder(path);
        }

        static string Quote(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

        static long Add(long total, long value) => total > long.MaxValue - value ? long.MaxValue : total + value;
    }
}
