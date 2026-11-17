using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.SkillTree.EditorTools
{
    public class SkillTreeGraphWindow : EditorWindow
    {
        [SerializeField] SkillTreeDefinition tree;

        SkillTreeGraphView graph;
        ScrollView inspector;
        Label status;
        ObjectField treeField;
        SerializedObject serializedTree;

        [MenuItem("Window/SMPro/Skill Tree Graph")]
        static void OpenEmpty() => GetWindow<SkillTreeGraphWindow>("Skill Tree Graph");

        public static void Open(SkillTreeDefinition definition)
        {
            var window = GetWindow<SkillTreeGraphWindow>("Skill Tree Graph");
            window.SetTree(definition);
            window.Focus();
        }

        void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;

        void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        void CreateGUI()
        {
            var toolbar = new Toolbar();
            treeField = new ObjectField { objectType = typeof(SkillTreeDefinition), allowSceneObjects = false, value = tree };
            treeField.style.minWidth = 240;
            treeField.RegisterValueChangedCallback(e => SetTree(e.newValue as SkillTreeDefinition));
            toolbar.Add(treeField);
            toolbar.Add(new ToolbarButton(() => graph.FrameAll()) { text = "Frame All" });
            toolbar.Add(new ToolbarButton(() => { if (tree != null) SkillTreeTableWindow.Open(tree); }) { text = "Table" });
            toolbar.Add(new ToolbarButton(() => { if (tree != null) Selection.activeObject = tree; }) { text = "Select Asset" });
            rootVisualElement.Add(toolbar);

            var split = new TwoPaneSplitView(1, 340, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;

            graph = new SkillTreeGraphView();
            graph.SelectionChanged += RebuildInspector;
            graph.TreeEdited += OnTreeEdited;
            split.Add(graph);

            inspector = new ScrollView();
            inspector.style.paddingLeft = 6;
            inspector.style.paddingRight = 6;
            split.Add(inspector);
            rootVisualElement.Add(split);

            status = new Label();
            status.style.paddingLeft = 6;
            status.style.paddingTop = 2;
            status.style.paddingBottom = 2;
            rootVisualElement.Add(status);

            Reload();
        }

        void SetTree(SkillTreeDefinition definition)
        {
            tree = definition;
            if (treeField != null) treeField.SetValueWithoutNotify(definition);
            titleContent = new GUIContent(definition != null ? definition.name : "Skill Tree Graph");
            Reload();
        }

        void Reload()
        {
            if (graph == null) return;
            serializedTree = tree != null ? new SerializedObject(tree) : null;
            graph.Load(tree);
            if (tree != null) graph.schedule.Execute(() => graph.FrameAll());
            RebuildInspector();
            UpdateStatus();
        }

        void OnUndoRedo()
        {
            if (tree == null || graph == null) return;
            tree.InvalidateLookups();
            serializedTree?.Update();
            graph.Load(tree);
            RebuildInspector();
            UpdateStatus();
        }

        void OnTreeEdited()
        {
            serializedTree?.Update();
            RebuildInspector();
            UpdateStatus();
        }

        void RebuildInspector()
        {
            if (inspector == null) return;
            inspector.Clear();

            if (tree == null)
            {
                inspector.Add(new HelpBox("Pick a Skill Tree Definition in the toolbar.", HelpBoxMessageType.Info));
                return;
            }

            SkillNodeElement selected = graph.SelectedNode;
            if (selected == null)
            {
                inspector.Add(new HelpBox("Select a node to edit it. Right-click the graph to add nodes; drag from Unlocks to Requires to link them.", HelpBoxMessageType.Info));
                return;
            }

            int index = tree.nodes.IndexOf(selected.Definition);
            if (index < 0) return;

            foreach (ValidationIssue issue in SkillTreeValidation.Validate(tree).Where(i => i.NodeId == selected.Definition.id))
                inspector.Add(new HelpBox(issue.Message, issue.Severity == IssueSeverity.Error ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning));

            string path = $"nodes.Array.data[{index}]";
            inspector.Add(new IMGUIContainer(() => DrawNode(path)));
        }

        void DrawNode(string path)
        {
            if (serializedTree == null || serializedTree.targetObject == null) return;
            serializedTree.Update();
            SerializedProperty node = serializedTree.FindProperty(path);
            if (node == null) return;

            EditorGUIUtility.labelWidth = 110;
            SerializedProperty end = node.GetEndProperty();
            SerializedProperty child = node.Copy();
            bool enter = true;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                using (new EditorGUI.DisabledScope(child.name == "id"))
                    EditorGUILayout.PropertyField(child, true);
            }

            if (!serializedTree.ApplyModifiedProperties()) return;
            tree.InvalidateLookups();
            graph.RebuildEdges();
            graph.RefreshNodes();
            UpdateStatus();
        }

        void UpdateStatus()
        {
            if (status == null) return;
            if (tree == null)
            {
                status.text = "";
                return;
            }
            var issues = SkillTreeValidation.Validate(tree);
            int errors = issues.Count(i => i.Severity == IssueSeverity.Error);
            int warnings = issues.Count - errors;
            status.text = $"{tree.nodes.Count} nodes · {tree.stats.Count} stats · {errors} error(s) · {warnings} warning(s)";
            status.style.color = errors > 0 ? new Color(1f, 0.45f, 0.45f) : warnings > 0 ? new Color(1f, 0.8f, 0.35f) : Color.gray;
        }
    }
}
