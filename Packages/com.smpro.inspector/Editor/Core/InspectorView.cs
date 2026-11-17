using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    internal sealed class InspectorView
    {
        readonly VisualElement root;
        readonly SerializedObject serializedObject;
        readonly Undo.UndoRedoCallback undoAction;
        readonly Action<PlayModeStateChange> playModeAction;
        List<StructureKey> structure = new();
        bool rebuildPending;

        readonly bool showScript;

        InspectorView(SerializedObject serializedObject, bool showScript)
        {
            this.serializedObject = serializedObject;
            this.showScript = showScript;
            root = new VisualElement { name = "SMInspectorPro" };
            root.userData = this;
            undoAction = Refresh;
            playModeAction = _ => Refresh();
            Build();
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            root.RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            root.RegisterCallback<DetachFromPanelEvent>(_ => OnDetach());
        }

        public InspectorRefresher Refresher { get; private set; }

        public static VisualElement Create(SerializedObject serializedObject, bool showScript = true) => new InspectorView(serializedObject, showScript).root;

        public static InspectorView Of(VisualElement root) => root?.userData as InspectorView;

        void Build()
        {
            root.Clear();
            InspectorBuilder.Result result = InspectorBuilder.BuildContent(root, serializedObject, RequestRebuild, showScript);
            Refresher = result.Refresher;
            structure = result.Structure;
            Refresher.Refresh();
        }

        public void Refresh()
        {
            if (serializedObject.targetObject == null) return;
            serializedObject.UpdateIfRequiredOrScript();
            if (StructureChanged()) Rebuild();
            else Refresher.Refresh();
        }

        bool StructureChanged()
        {
            foreach (StructureKey key in structure)
                if (key.Changed) return true;
            return false;
        }

        public void RequestRebuild()
        {
            if (root.panel == null)
            {
                Rebuild();
                return;
            }
            if (rebuildPending) return;
            rebuildPending = true;
            root.schedule.Execute(() =>
            {
                rebuildPending = false;
                Rebuild();
            });
        }

        void Rebuild()
        {
            bool attached = root.panel != null;
            Refresher.Stop();
            serializedObject.UpdateIfRequiredOrScript();
            Build();
            if (attached) Refresher.Start(root);
        }

        void OnAttach()
        {
            Undo.undoRedoPerformed += undoAction;
            EditorApplication.playModeStateChanged += playModeAction;
            Refresher.Start(root);
        }

        void OnDetach()
        {
            Undo.undoRedoPerformed -= undoAction;
            EditorApplication.playModeStateChanged -= playModeAction;
            Refresher.Stop();
        }
    }
}
