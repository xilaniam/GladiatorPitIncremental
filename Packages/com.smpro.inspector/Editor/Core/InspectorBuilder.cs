using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.EditorTools
{
    internal static class InspectorBuilder
    {
        const string ScriptPath = "m_Script";
        const string StylePath = "Packages/com.smpro.inspector/Editor/Styles/SMInspector.uss";

        static readonly HashSet<string> reportedErrors = new();
        static StyleSheet styles;

        public static VisualElement Build(SerializedObject serializedObject)
        {
            var root = new VisualElement { name = "SMInspectorPro" };
            AddStyles(root);
            TypeModel model = TypeModel.For(serializedObject.targetObject.GetType());
            Object[] targets = serializedObject.targetObjects;
            var properties = new List<InspectorProperty>();

            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                SerializedProperty property = iterator.Copy();
                if (property.propertyPath == ScriptPath) root.Add(ScriptField(property));
                else properties.Add(BuildProperty(property, model, null, targets));
            }
            GroupLayout.Arrange(root, model.Type, properties);
            return root;
        }

        static void AddStyles(VisualElement root)
        {
            if (styles == null) styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (styles != null) root.styleSheets.Add(styles);
        }

        static VisualElement ScriptField(SerializedProperty property)
        {
            var field = new PropertyField(property) { name = FieldName(property) };
            field.SetEnabled(false);
            return field;
        }

        static InspectorProperty BuildProperty(SerializedProperty serializedProperty, TypeModel ownerModel, InspectorProperty parent, Object[] targets)
        {
            MemberModel member = ownerModel?.Find(serializedProperty.name);
            var property = new InspectorProperty(serializedProperty, member, ownerModel?.Type, parent, targets);
            property.Element = CreateElement(property);
            return property;
        }

        static VisualElement CreateElement(InspectorProperty property)
        {
            IFieldHandler field = CreateHandlers(property);
            if (field != null)
            {
                try
                {
                    VisualElement custom = field.CreateField();
                    if (custom != null) return custom;
                }
                catch (Exception exception)
                {
                    return WithError(DefaultField(property.SerializedProperty), Report(property, field.GetType(), exception));
                }
            }

            Type nestedType = TakenOverNestedType(property);
            return nestedType != null ? NestedFoldout(property, TypeModel.For(nestedType)) : DefaultField(property.SerializedProperty);
        }

        static IFieldHandler CreateHandlers(InspectorProperty property)
        {
            if (property.Member == null) return null;
            IFieldHandler field = null;
            foreach (InspectorAttribute attribute in property.Member.Attributes)
            {
                if (attribute is GroupAttribute) continue;
                AttributeHandler handler;
                try
                {
                    handler = HandlerRegistry.Create(property, attribute);
                }
                catch (Exception exception)
                {
                    Report(property, HandlerRegistry.HandlerTypeFor(attribute.GetType()), exception);
                    continue;
                }
                if (handler == null) continue;
                property.AddHandler(handler);
                if (handler is IFieldHandler candidate && (field == null || candidate.Priority > field.Priority)) field = candidate;
            }
            return field;
        }

        static Type TakenOverNestedType(InspectorProperty property)
        {
            SerializedProperty serializedProperty = property.SerializedProperty;
            MemberModel member = property.Member;
            if (member == null || serializedProperty.propertyType != SerializedPropertyType.Generic || serializedProperty.isArray) return null;
            Type type = member.ValueType;
            if (!TypeModel.For(type).UsesInspectorAttributes || PropertyDrawerLookup.HasDrawerFor(type)) return null;
            foreach (PropertyAttribute attribute in member.Info.GetCustomAttributes<PropertyAttribute>(true))
                if (PropertyDrawerLookup.HasDrawerFor(attribute.GetType())) return null;
            return type;
        }

        static VisualElement NestedFoldout(InspectorProperty property, TypeModel nestedModel)
        {
            SerializedProperty serializedProperty = property.SerializedProperty;
            var container = new VisualElement { name = FieldName(serializedProperty) };
            AddUnityDecorators(container, property.Member.Info);

            var foldout = new Foldout { text = serializedProperty.displayName, tooltip = serializedProperty.tooltip, value = serializedProperty.isExpanded };
            foldout.RegisterValueChangedCallback(change =>
            {
                if (change.target == foldout) serializedProperty.isExpanded = change.newValue;
            });
            container.Add(foldout);

            var children = new List<InspectorProperty>();
            SerializedProperty child = serializedProperty.Copy();
            SerializedProperty end = serializedProperty.GetEndProperty();
            for (bool enterChildren = true; child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end); enterChildren = false)
                children.Add(BuildProperty(child.Copy(), nestedModel, property, property.Targets));
            GroupLayout.Arrange(foldout.contentContainer, nestedModel.Type, children);
            return container;
        }

        static void AddUnityDecorators(VisualElement container, MemberInfo member)
        {
            foreach (PropertyAttribute attribute in member.GetCustomAttributes<PropertyAttribute>(true))
            {
                switch (attribute)
                {
                    case SpaceAttribute space:
                        container.Add(new VisualElement { style = { height = space.height } });
                        break;
                    case HeaderAttribute header:
                        var label = new Label(header.header);
                        label.AddToClassList("unity-header-drawer__label");
                        container.Add(label);
                        break;
                }
            }
        }

        static PropertyField DefaultField(SerializedProperty serializedProperty) => new(serializedProperty) { name = FieldName(serializedProperty) };

        static VisualElement WithError(VisualElement field, string message)
        {
            var container = new VisualElement { name = field.name };
            field.name = null;
            container.Add(field);
            container.Add(new HelpBox(message, HelpBoxMessageType.Error));
            return container;
        }

        internal static string Report(InspectorProperty property, Type handlerType, Exception exception)
        {
            string message = $"{handlerType?.Name ?? "Handler"} failed on '{property.Name}': {exception.Message}";
            string key = $"{property.Member?.Info.DeclaringType?.FullName}.{property.Name}.{handlerType?.FullName}";
            if (reportedErrors.Add(key)) Debug.LogException(exception);
            return message;
        }

        static string FieldName(SerializedProperty serializedProperty) => "PropertyField:" + serializedProperty.propertyPath;

        internal static void ClearReportedErrors()
        {
            reportedErrors.Clear();
            GroupLayout.ClearReported();
        }
    }
}
