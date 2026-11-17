using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMPro.SkillTree.UI
{
    public static class TooltipText
    {
        public static string Build(SkillTreeController controller, SkillNodeDefinition node, NodeStateStyle style, bool flavourTextReplacesEffects)
        {
            NodeState state = controller.GetState(node.id);
            if (state == NodeState.Mystery) return $"{style.mysteryTitle}\n{style.mysteryText}";

            SkillTreeDefinition tree = controller.Definition;
            int level = controller.GetLevel(node.id);
            var text = new StringBuilder();
            text.AppendLine(node.DisplayName);

            bool hasFlavour = !string.IsNullOrEmpty(node.flavourText);
            if (hasFlavour) text.AppendLine(node.flavourText);
            if (!hasFlavour || !flavourTextReplacesEffects)
            {
                foreach (string line in EffectLines(tree, node, level)) text.AppendLine(line);
            }

            text.AppendLine(state == NodeState.Maxed ? "MAX" : $"Cost: {controller.GetCost(node.id)}");
            text.Append($"Level {level}/{node.maxLevel}");

            List<string> missing = node.requirements
                .Where(r => controller.GetLevel(r.nodeId) < r.level)
                .Select(r => $"{NodeName(tree, r.nodeId)} Lv{r.level}")
                .Concat(node.conditions.Where(c => c != null && !c.IsMet(controller)).Select(c => c.Describe(tree)))
                .ToList();
            if (missing.Count > 0) text.Append("\nRequires: ").Append(string.Join(", ", missing));

            return text.ToString();
        }

        static IEnumerable<string> EffectLines(SkillTreeDefinition tree, SkillNodeDefinition node, int level)
        {
            foreach (ISkillEffect effect in node.effects)
            {
                if (effect == null) continue;

                string next = level < node.maxLevel ? effect.Describe(tree, level + 1) : null;
                string current = level > 0 ? effect.Describe(tree, level) : null;

                string line = current == null ? next
                    : next == null || next == current ? current
                    : $"{current} -> {next}";
                if (!string.IsNullOrEmpty(line)) yield return line;
            }
        }

        static string NodeName(SkillTreeDefinition tree, string nodeId) =>
            tree.TryGetNode(nodeId, out SkillNodeDefinition node) ? node.DisplayName : nodeId;
    }
}
