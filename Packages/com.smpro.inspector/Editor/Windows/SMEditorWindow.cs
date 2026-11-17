using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    public abstract class SMEditorWindow : EditorWindow
    {
        SerializedObject serializedWindow;

        protected VisualElement Inspector { get; private set; }

        protected virtual void CreateGUI() => RebuildInspector();

        protected void RebuildInspector()
        {
            rootVisualElement.Clear();
            serializedWindow = new SerializedObject(this);
            Inspector = InspectorBuilder.Build(serializedWindow, false);
            Inspector.Bind(serializedWindow);

            var scroll = new ScrollView { name = "sm-window-scroll" };
            scroll.style.flexGrow = 1;
            scroll.contentContainer.style.paddingLeft = 4;
            scroll.contentContainer.style.paddingRight = 4;
            scroll.Add(Inspector);
            rootVisualElement.Add(scroll);
        }

        protected virtual void OnDisable() => serializedWindow?.Dispose();
    }
}
