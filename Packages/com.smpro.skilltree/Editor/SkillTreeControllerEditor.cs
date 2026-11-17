using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SMPro.SkillTree.EditorTools
{
    [CustomEditor(typeof(SkillTreeController))]
    public class SkillTreeControllerEditor : Editor
    {
        long grantAmount = 1000;
        string grantCurrency = CurrencyIds.Default;
        bool showNodes = true;
        bool showStats = true;
        string nodeFilter = "";
        readonly HashSet<string> expandedStats = new();

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (SkillTreeController)target;
            if (controller.Definition == null) return;

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Graph")) SkillTreeGraphWindow.Open(controller.Definition);

            if (!Application.isPlaying)
            {
                int errors = SkillTreeValidation.Validate(controller.Definition).Count(i => i.Severity == IssueSeverity.Error);
                if (errors > 0) EditorGUILayout.HelpBox($"{controller.Definition.name} has {errors} error(s). Select the asset to see them.", MessageType.Error);
                return;
            }

            DrawDebugTools(controller);
            DrawNodes(controller);
            DrawStats(controller);
        }

        void DrawDebugTools(SkillTreeController controller)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play Mode", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                grantCurrency = EditorGUILayout.TextField(grantCurrency, GUILayout.Width(100));
                grantAmount = EditorGUILayout.LongField(grantAmount);
                using (new EditorGUI.DisabledScope(controller.Wallet == null))
                    if (GUILayout.Button("Grant", GUILayout.Width(60))) controller.Wallet.Add(grantCurrency, grantAmount);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Max Everything")) MaxEverything(controller);
                if (GUILayout.Button("Reset (refund)")) controller.ResetTree();
                if (GUILayout.Button("Reset (no refund)")) controller.ResetTree(0f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy Save JSON")) EditorGUIUtility.systemCopyBuffer = controller.CaptureState().ToJson();
                if (GUILayout.Button("Load Save JSON"))
                {
                    try
                    {
                        controller.RestoreState(SkillTreeState.FromJson(EditorGUIUtility.systemCopyBuffer));
                    }
                    catch (System.ArgumentException)
                    {
                        Debug.LogWarning("The clipboard does not hold skill tree save JSON.");
                    }
                }
            }
            EditorGUILayout.LabelField($"Levels bought: {controller.TotalLevels}", EditorStyles.miniLabel);
        }

        void DrawNodes(SkillTreeController controller)
        {
            showNodes = EditorGUILayout.Foldout(showNodes, "Nodes", true);
            if (!showNodes) return;

            nodeFilter = EditorGUILayout.TextField("Filter", nodeFilter);
            foreach (SkillNodeDefinition node in controller.Definition.nodes.Where(n => n != null))
            {
                if (!string.IsNullOrEmpty(nodeFilter) && node.DisplayName.IndexOf(nodeFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                int level = controller.GetLevel(node.id);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(node.DisplayName, GUILayout.MinWidth(120));
                    EditorGUILayout.LabelField(controller.GetState(node.id).ToString(), GUILayout.Width(70));
                    EditorGUILayout.LabelField($"{level}/{node.maxLevel}", GUILayout.Width(40));
                    using (new EditorGUI.DisabledScope(level >= node.maxLevel))
                        if (GUILayout.Button("+1", GUILayout.Width(32))) BuyFree(controller, node.id);
                    using (new EditorGUI.DisabledScope(!controller.CanRefund(node.id)))
                        if (GUILayout.Button("Refund", GUILayout.Width(56))) controller.TryRefund(node.id);
                }
            }
        }

        void DrawStats(SkillTreeController controller)
        {
            showStats = EditorGUILayout.Foldout(showStats, "Stats", true);
            if (!showStats) return;

            Dictionary<object, string> sources = EffectOwners(controller.Definition);
            foreach (Stat stat in controller.Stats.All)
            {
                bool expanded = expandedStats.Contains(stat.Id);
                string label = $"{stat.Definition.DisplayName} = {stat.Value:0.###}  (base {stat.Definition.baseValue:0.###}, {stat.Modifiers.Count} modifier(s))";
                bool next = EditorGUILayout.Foldout(expanded, label, true);
                if (next != expanded)
                {
                    if (next) expandedStats.Add(stat.Id);
                    else expandedStats.Remove(stat.Id);
                }
                if (!next) continue;

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (StatModifier modifier in stat.Modifiers)
                    {
                        string from = modifier.Source == null ? "unknown"
                            : sources.TryGetValue(modifier.Source, out string node) ? node
                            : modifier.Source is Object unityObject ? unityObject.name
                            : modifier.Source.GetType().Name;
                        string expiry = modifier.IsTemporary ? $"  (ends at {modifier.ExpiresAt:0.0}s)" : "";
                        EditorGUILayout.LabelField($"{modifier.Kind} {modifier.Value:0.###}  from {from}{expiry}", EditorStyles.miniLabel);
                    }
                }
            }
        }

        static void BuyFree(SkillTreeController controller, string nodeId)
        {
            if (controller.CanPurchase(nodeId) == PurchaseCheck.Funds && controller.Definition.TryGetNode(nodeId, out SkillNodeDefinition node))
                controller.Wallet?.Add(node.currencyId, controller.GetCost(nodeId));

            PurchaseCheck check = controller.CanPurchase(nodeId);
            if (check == PurchaseCheck.Ok) controller.TryPurchase(nodeId);
            else Debug.Log($"[SkillTree] Cannot buy {nodeId}: {check}.");
        }

        static void MaxEverything(SkillTreeController controller)
        {
            bool progress = true;
            while (progress)
            {
                progress = false;
                foreach (SkillNodeDefinition node in controller.Definition.nodes.Where(n => n != null))
                {
                    int before = controller.GetLevel(node.id);
                    if (before >= node.maxLevel) continue;
                    BuyFreeQuiet(controller, node);
                    progress |= controller.GetLevel(node.id) > before;
                }
            }
        }

        static void BuyFreeQuiet(SkillTreeController controller, SkillNodeDefinition node)
        {
            if (controller.CanPurchase(node.id) == PurchaseCheck.Funds) controller.Wallet?.Add(node.currencyId, controller.GetCost(node.id));
            controller.TryPurchase(node.id);
        }

        static Dictionary<object, string> EffectOwners(SkillTreeDefinition tree)
        {
            var owners = new Dictionary<object, string>();
            foreach (SkillNodeDefinition node in tree.nodes.Where(n => n != null))
                foreach (ISkillEffect effect in node.effects) AddOwner(owners, effect, node.DisplayName);
            return owners;
        }

        static void AddOwner(Dictionary<object, string> owners, ISkillEffect effect, string nodeName)
        {
            if (effect == null) return;
            owners[effect] = nodeName;
            if (effect is CompositeEffect composite)
                foreach (ISkillEffect child in composite.effects) AddOwner(owners, child, nodeName);
        }
    }
}
