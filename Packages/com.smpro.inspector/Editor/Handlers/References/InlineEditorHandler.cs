using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(InlineEditorAttribute))]
    public sealed class InlineEditorHandler : DecoratorHandler<InlineEditorAttribute>, IRefreshable
    {
        public const string ContainerClass = "sm-inline-editor";
        public const int MaxDepth = 3;

        Foldout box;
        Object shown;
        bool built;

        public override int Order => 5;
        public bool NeedsPolling => false;
        public Foldout Box => box;

        protected override void OnInitialize()
        {
            if (Property.SerializedProperty?.propertyType != SerializedPropertyType.ObjectReference)
                ReportProblem("InlineEditor: only works on object references");
        }

        public override VisualElement Decorate(VisualElement field)
        {
            if (Problems.Count > 0) return field;
            SerializedProperty property = Property.SerializedProperty;
            box = new Foldout { name = "sm-inline:" + Property.Path, value = Attribute.Expanded };
            box.AddToClassList(ContainerClass);
            box.RegisterCallback<AttachToPanelEvent>(_ => Show(property.objectReferenceValue, true));
            Show(property.objectReferenceValue, false);
            return Stack(field, box);
        }

        public void Refresh()
        {
            if (box == null) return;
            Object value = Property.SerializedProperty.objectReferenceValue;
            if (value != shown || !built) Show(value, false);
        }

        void Show(Object value, bool force)
        {
            if (!force && built && value == shown) return;
            shown = value;
            box.Clear();
            box.style.display = value == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (value == null)
            {
                built = true;
                return;
            }
            box.text = value.name;

            Object target = Property.TargetCount > 0 ? Property.Targets[0] : null;
            if (value == target)
            {
                box.Add(new HelpBox("An object can't be shown inside its own inspector", HelpBoxMessageType.Info));
                built = true;
                return;
            }
            if (box.panel == null)
            {
                built = false;
                return;
            }
            if (Depth(box) > MaxDepth)
            {
                box.Add(new HelpBox("Nested too deep to show here", HelpBoxMessageType.Info));
                built = true;
                return;
            }
            box.Add(new InspectorElement(value));
            built = true;
        }

        static int Depth(VisualElement element)
        {
            int depth = 0;
            for (VisualElement current = element; current != null; current = current.parent)
                if (current.ClassListContains(ContainerClass)) depth++;
            return depth;
        }
    }
}
