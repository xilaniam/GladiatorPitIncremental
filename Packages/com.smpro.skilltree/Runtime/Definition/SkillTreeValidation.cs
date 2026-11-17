using System.Collections.Generic;
using System.Linq;

namespace SMPro.SkillTree
{
    public enum IssueSeverity
    {
        Warning,
        Error
    }

    public readonly struct ValidationIssue
    {
        public IssueSeverity Severity { get; }
        public string NodeId { get; }
        public string Message { get; }

        public ValidationIssue(IssueSeverity severity, string nodeId, string message)
        {
            Severity = severity;
            NodeId = nodeId;
            Message = message;
        }

        public override string ToString() => string.IsNullOrEmpty(NodeId) ? Message : $"{NodeId}: {Message}";
    }

    public static class SkillTreeValidation
    {
        public static List<ValidationIssue> Validate(SkillTreeDefinition tree)
        {
            var issues = new List<ValidationIssue>();
            if (tree == null) return issues;

            if (string.IsNullOrEmpty(tree.treeId)) Error(issues, null, "The tree has no tree id; save files need one.");

            ValidateStats(tree, issues);
            Dictionary<string, SkillNodeDefinition> nodes = ValidateNodeIds(tree, issues);
            foreach (SkillNodeDefinition node in tree.nodes.Where(n => n != null)) ValidateNode(tree, node, nodes, issues);
            ValidateCycles(nodes, issues);

            return issues;
        }

        static void ValidateStats(SkillTreeDefinition tree, List<ValidationIssue> issues)
        {
            var ids = new HashSet<string>();
            foreach (StatDefinition stat in tree.stats)
            {
                if (stat == null) continue;
                if (string.IsNullOrEmpty(stat.id)) Error(issues, null, "A stat has no id.");
                else if (!ids.Add(stat.id)) Error(issues, null, $"Stat id '{stat.id}' is used twice.");
                if (stat != null && stat.min > stat.max) Error(issues, null, $"Stat '{stat.id}' has min above max.");
            }
        }

        static Dictionary<string, SkillNodeDefinition> ValidateNodeIds(SkillTreeDefinition tree, List<ValidationIssue> issues)
        {
            var nodes = new Dictionary<string, SkillNodeDefinition>();
            foreach (SkillNodeDefinition node in tree.nodes)
            {
                if (node == null)
                {
                    Error(issues, null, "The node list has an empty entry.");
                    continue;
                }
                if (string.IsNullOrEmpty(node.id)) Error(issues, null, $"Node '{node.displayName}' has no id.");
                else if (nodes.ContainsKey(node.id)) Error(issues, node.id, "Node id is used twice; save files would mix them up.");
                else nodes.Add(node.id, node);
            }
            return nodes;
        }

        static void ValidateNode(SkillTreeDefinition tree, SkillNodeDefinition node, Dictionary<string, SkillNodeDefinition> nodes, List<ValidationIssue> issues)
        {
            string id = node.id;

            if (node.maxLevel < 1) Error(issues, id, "Max level must be at least 1.");
            if (node.cost == null) Warning(issues, id, "Has no cost curve, so every level is free.");
            if (node.icon == null) Warning(issues, id, "Has no icon.");
            if (string.IsNullOrEmpty(node.currencyId)) Error(issues, id, "Has no currency id.");

            foreach (NodeRequirement requirement in node.requirements)
            {
                if (requirement == null || string.IsNullOrEmpty(requirement.nodeId))
                {
                    Error(issues, id, "A requirement has no node selected.");
                    continue;
                }
                if (requirement.nodeId == id)
                {
                    Error(issues, id, "Requires itself.");
                    continue;
                }
                if (!nodes.TryGetValue(requirement.nodeId, out SkillNodeDefinition parent))
                {
                    Error(issues, id, $"Requires unknown node '{requirement.nodeId}'.");
                    continue;
                }
                if (requirement.level < 1) Error(issues, id, $"Requirement on {parent.DisplayName} needs level 1 or more.");
                if (requirement.level > parent.maxLevel) Error(issues, id, $"Requires {parent.DisplayName} level {requirement.level}, but its max level is {parent.maxLevel}.");
            }

            if (node.effects.Count == 0) Warning(issues, id, "Has no effects.");
            foreach (ISkillEffect effect in node.effects)
            {
                if (effect == null) Error(issues, id, "An effect slot is empty or its class no longer exists.");
                else foreach (string problem in effect.Validate(tree)) Error(issues, id, problem);
            }

            foreach (ISkillCondition condition in node.conditions)
            {
                if (condition == null) Error(issues, id, "A condition slot is empty or its class no longer exists.");
                else foreach (string problem in condition.Validate(tree)) Error(issues, id, problem);
            }
        }

        static void ValidateCycles(Dictionary<string, SkillNodeDefinition> nodes, List<ValidationIssue> issues)
        {
            var state = new Dictionary<string, int>();
            var stack = new List<string>();

            foreach (string id in nodes.Keys) Visit(id);

            void Visit(string id)
            {
                state.TryGetValue(id, out int current);
                if (current == 2) return;
                if (current == 1)
                {
                    int start = stack.IndexOf(id);
                    List<string> cycle = stack.Skip(start).Append(id).ToList();
                    Error(issues, id, $"Requirement cycle: {string.Join(" -> ", cycle)}. None of these nodes can ever be bought.");
                    return;
                }

                state[id] = 1;
                stack.Add(id);
                foreach (NodeRequirement requirement in nodes[id].requirements)
                {
                    if (requirement != null && requirement.nodeId != null && nodes.ContainsKey(requirement.nodeId) && requirement.nodeId != id)
                        Visit(requirement.nodeId);
                }
                stack.RemoveAt(stack.Count - 1);
                state[id] = 2;
            }
        }

        static void Error(List<ValidationIssue> issues, string nodeId, string message) =>
            issues.Add(new ValidationIssue(IssueSeverity.Error, nodeId, message));

        static void Warning(List<ValidationIssue> issues, string nodeId, string message) =>
            issues.Add(new ValidationIssue(IssueSeverity.Warning, nodeId, message));
    }
}
