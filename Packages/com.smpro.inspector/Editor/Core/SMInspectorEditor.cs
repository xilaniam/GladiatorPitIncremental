using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [CanEditMultipleObjects]
    public class SMInspectorEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            if (target != null && TypeModel.For(target.GetType()).UsesInspectorAttributes)
                return InspectorBuilder.Build(serializedObject);

            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }

    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true), CanEditMultipleObjects]
    internal sealed class SMBehaviourEditor : SMInspectorEditor
    {
    }

    [CustomEditor(typeof(ScriptableObject), true, isFallback = true), CanEditMultipleObjects]
    internal sealed class SMScriptableObjectEditor : SMInspectorEditor
    {
    }
}
