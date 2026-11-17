using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.SkillTree.EditorTools
{
    internal class SkillTreeGraphView : GraphView
    {
        readonly Dictionary<string, SkillNodeElement> elements = new();
        SkillTreeDefinition tree;
        Vector2 contextMenuPosition;
        bool suppressChanges;

        public event Action SelectionChanged;
        public event Action TreeEdited;

        public SkillNodeElement SelectedNode => selection.OfType<SkillNodeElement>().FirstOrDefault();

        public SkillTreeGraphView()
        {
            SetupZoom(0.1f, 2f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            style.flexGrow = 1;

            graphViewChanged = OnGraphViewChanged;
        }

        public void Load(SkillTreeDefinition definition)
        {
            tree = definition;
            suppressChanges = true;
            DeleteElements(graphElements.ToList());
            elements.Clear();

            if (tree != null)
            {
                foreach (SkillNodeDefinition node in tree.nodes.Where(n => n != null && !string.IsNullOrEmpty(n.id)))
                {
                    var element = new SkillNodeElement(node);
                    element.SetPosition(new Rect(ToGraph(node.position), Vector2.zero));
                    AddElement(element);
                    elements[node.id] = element;
                }
                RebuildEdges();
                RefreshNodes();
            }
            suppressChanges = false;
        }

        public void RebuildEdges()
        {
            suppressChanges = true;
            DeleteElements(edges.ToList());
            foreach (SkillNodeElement child in elements.Values)
            {
                foreach (NodeRequirement requirement in child.Definition.requirements)
                {
                    if (requirement == null || !elements.TryGetValue(requirement.nodeId ?? "", out SkillNodeElement parent)) continue;
                    Edge edge = parent.Output.ConnectTo(child.Input);
                    edge.userData = requirement;
                    edge.tooltip = requirement.level > 1 ? $"Needs level {requirement.level}" : "";
                    AddElement(edge);
                }
            }
            suppressChanges = false;
        }

        public void RefreshNodes()
        {
            if (tree == null) return;
            Dictionary<string, IssueSeverity> worst = SkillTreeValidation.Validate(tree)
                .Where(i => !string.IsNullOrEmpty(i.NodeId))
                .GroupBy(i => i.NodeId)
                .ToDictionary(g => g.Key, g => g.Max(i => i.Severity));

            foreach (SkillNodeElement element in elements.Values)
                element.Refresh(tree, worst.TryGetValue(element.Definition.id, out IssueSeverity severity) ? severity : null);
        }

        public void Select(string nodeId)
        {
            if (!elements.TryGetValue(nodeId, out SkillNodeElement element)) return;
            ClearSelection();
            AddToSelection(element);
            FrameSelection();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) =>
            ports.Where(p => p.direction != startPort.direction && p.node != startPort.node).ToList();

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (tree == null) return;
            contextMenuPosition = contentViewContainer.WorldToLocal(evt.mousePosition);

            evt.menu.AppendAction("Add Skill Node", _ => AddNode(contextMenuPosition));
            if (evt.target is SkillNodeElement element)
                evt.menu.AppendAction("Duplicate Node", _ => DuplicateNode(element));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", _ => DeleteSelection(), selection.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            SelectionChanged?.Invoke();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            SelectionChanged?.Invoke();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            SelectionChanged?.Invoke();
        }

        void AddNode(Vector2 graphPosition)
        {
            Undo.RecordObject(tree, "Add Skill Node");
            var node = new SkillNodeDefinition
            {
                id = SkillTreeDefinition.NewId(),
                displayName = "New Skill",
                position = ToTree(graphPosition)
            };
            tree.nodes.Add(node);
            CommitAndReload(node.id);
        }

        void DuplicateNode(SkillNodeElement source)
        {
            Undo.RecordObject(tree, "Duplicate Skill Node");
            SkillNodeDefinition copy = ManagedClone.Clone(source.Definition);
            copy.id = SkillTreeDefinition.NewId();
            copy.displayName = source.Definition.DisplayName + " Copy";
            copy.position = source.Definition.position + new Vector2(60f, -60f);
            tree.nodes.Add(copy);
            CommitAndReload(copy.id);
        }

        void CommitAndReload(string selectId)
        {
            Commit();
            Load(tree);
            Select(selectId);
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (suppressChanges || tree == null) return change;
            Undo.RecordObject(tree, "Edit Skill Tree");

            if (change.elementsToRemove != null)
            {
                foreach (GraphElement element in change.elementsToRemove)
                {
                    if (element is Edge edge && edge.userData is NodeRequirement requirement && edge.input?.node is SkillNodeElement child)
                        child.Definition.requirements.Remove(requirement);
                }
                foreach (SkillNodeElement node in change.elementsToRemove.OfType<SkillNodeElement>())
                {
                    tree.nodes.Remove(node.Definition);
                    elements.Remove(node.Definition.id);
                    foreach (SkillNodeDefinition other in tree.nodes.Where(n => n != null))
                        other.requirements.RemoveAll(r => r != null && r.nodeId == node.Definition.id);
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate.ToList())
                {
                    var parent = (SkillNodeElement)edge.output.node;
                    var child = (SkillNodeElement)edge.input.node;
                    bool duplicate = child.Definition.requirements.Any(r => r != null && r.nodeId == parent.Definition.id);
                    if (duplicate || CreatesCycle(parent.Definition.id, child.Definition.id))
                    {
                        change.edgesToCreate.Remove(edge);
                        continue;
                    }
                    var requirement = new NodeRequirement { nodeId = parent.Definition.id, level = 1 };
                    child.Definition.requirements.Add(requirement);
                    edge.userData = requirement;
                }
            }

            if (change.movedElements != null)
            {
                foreach (SkillNodeElement node in change.movedElements.OfType<SkillNodeElement>())
                {
                    Vector2 moved = ToTree(new Vector2(node.style.left.value.value, node.style.top.value.value)) + Vector2.zero;
                    if (moved != node.Definition.position) node.Definition.position = moved;
                }
            }

            Commit();
            schedule.Execute(RefreshNodes);
            return change;
        }

        bool CreatesCycle(string parentId, string childId)
        {
            var pending = new Stack<string>();
            var seen = new HashSet<string>();
            pending.Push(parentId);
            while (pending.Count > 0)
            {
                string id = pending.Pop();
                if (id == childId) return true;
                if (!seen.Add(id) || !tree.TryGetNode(id, out SkillNodeDefinition node)) continue;
                foreach (NodeRequirement requirement in node.requirements)
                    if (requirement != null && requirement.nodeId != null) pending.Push(requirement.nodeId);
            }
            return false;
        }

        void Commit()
        {
            tree.InvalidateLookups();
            EditorUtility.SetDirty(tree);
            TreeEdited?.Invoke();
        }

        static Vector2 ToGraph(Vector2 treePosition) => new(Mathf.Round(treePosition.x), Mathf.Round(-treePosition.y));

        static Vector2 ToTree(Vector2 graphPosition) => new(Mathf.Round(graphPosition.x), Mathf.Round(-graphPosition.y));
    }
}
