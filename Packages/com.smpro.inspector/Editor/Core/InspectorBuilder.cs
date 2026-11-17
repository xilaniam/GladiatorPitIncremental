using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.EditorTools
{
    internal sealed class InspectorBuilder
    {
        const string ScriptPath = "m_Script";
        const string PropertyContainer = "sm-property-container";
        const string StylePath = "Packages/com.smpro.inspector/Editor/Styles/SMInspector.uss";

        static readonly HashSet<string> reportedErrors = new();
        static readonly HashSet<string> reportedProblems = new();
        static StyleSheet styles;

        readonly List<InspectorProperty> built = new();
        readonly List<Collapsible> collapsibles = new();

        InspectorBuilder()
        {
        }

        public static VisualElement Build(SerializedObject serializedObject)
        {
            var root = new VisualElement { name = "SMInspectorPro" };
            AddStyles(root);
            var builder = new InspectorBuilder();
            TypeModel model = TypeModel.For(serializedObject.targetObject.GetType());
            Object[] targets = serializedObject.targetObjects;
            var properties = new List<InspectorProperty>();

            VisualElement topSlot = null;
            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                SerializedProperty property = iterator.Copy();
                if (property.propertyPath == ScriptPath) root.Add(ScriptField(property));
                else properties.Add(builder.BuildProperty(property, model, null, targets));
            }
            if (typeof(ISelfValidator).IsAssignableFrom(model.Type))
            {
                topSlot = ValidationSlot("root");
                root.Add(topSlot);
            }
            builder.AddMembers(model, null, targets, properties);
            builder.Arrange(root, model.Type, properties);
            var validation = new InspectorValidation(serializedObject, builder.built, topSlot);
            root.userData = InspectorRefresher.Attach(root, serializedObject, builder.built, builder.collapsibles, validation);
            return root;
        }

        static VisualElement ValidationSlot(string path)
        {
            var slot = new VisualElement { name = "sm-validation:" + path };
            slot.AddToClassList("sm-validation");
            return slot;
        }

        internal static InspectorRefresher RefresherOf(VisualElement root) => root?.userData as InspectorRefresher;

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

        void Arrange(VisualElement container, Type ownerType, List<InspectorProperty> properties)
        {
            List<InspectorProperty> ordered = InDeclarationOrder(properties);
            collapsibles.AddRange(GroupLayout.Arrange(container, ownerType, ordered));
        }

        static List<InspectorProperty> InDeclarationOrder(List<InspectorProperty> properties)
        {
            var keyed = new List<(int key, int index, InspectorProperty property)>(properties.Count);
            int previous = -1;
            for (int i = 0; i < properties.Count; i++)
            {
                int key = properties[i].Member?.DeclarationOrder ?? previous;
                previous = key;
                keyed.Add((key, i, properties[i]));
            }
            keyed.Sort((a, b) => a.key != b.key ? a.key.CompareTo(b.key) : a.index.CompareTo(b.index));
            return keyed.ConvertAll(entry => entry.property);
        }

        void AddMembers(TypeModel model, InspectorProperty parent, Object[] targets, List<InspectorProperty> properties)
        {
            foreach (MemberModel member in model.Members)
            {
                if (member.Kind == MemberKind.SerializedField) continue;
                bool drawn = member.Kind == MemberKind.Method ? member.Has<ButtonAttribute>() : member.Has<ShowInInspectorAttribute>();
                if (drawn) properties.Add(BuildMember(member, model, parent, targets));
            }
        }

        InspectorProperty BuildProperty(SerializedProperty serializedProperty, TypeModel ownerModel, InspectorProperty parent, Object[] targets)
        {
            MemberModel member = ownerModel?.Find(serializedProperty.name);
            var property = new InspectorProperty(serializedProperty, member, ownerModel?.Type, parent, targets)
            {
                Label = LabelFor(serializedProperty.displayName, member)
            };
            return Finish(property);
        }

        InspectorProperty BuildMember(MemberModel member, TypeModel ownerModel, InspectorProperty parent, Object[] targets)
        {
            var property = new InspectorProperty(null, member, ownerModel.Type, parent, targets)
            {
                Label = LabelFor(ObjectNames.NicifyVariableName(member.Name), member)
            };
            return Finish(property);
        }

        InspectorProperty Finish(InspectorProperty property)
        {
            IFieldHandler fieldHandler = CreateHandlers(property);
            VisualElement field = CreateField(property, fieldHandler);
            NotifyFieldCreated(property, field);
            VisualElement element = WithProblems(property, Decorate(property, field));
            property.Element = NeedsValidationSlot(property) ? WithValidationSlot(property, element) : element;
            built.Add(property);
            return property;
        }

        static void NotifyFieldCreated(InspectorProperty property, VisualElement field)
        {
            foreach (AttributeHandler handler in property.Handlers)
            {
                try
                {
                    handler.OnFieldCreated(field);
                }
                catch (Exception exception)
                {
                    Report(property, handler.GetType(), exception);
                }
            }
        }

        static bool NeedsValidationSlot(InspectorProperty property)
        {
            if (property.SerializedProperty == null) return false;
            foreach (AttributeHandler handler in property.Handlers)
                if (handler is IValidatorHandler) return true;
            return typeof(ISelfValidator).IsAssignableFrom(property.OwnerType) || typeof(ISelfValidator).IsAssignableFrom(property.Member?.ValueType);
        }

        static VisualElement WithValidationSlot(InspectorProperty property, VisualElement element)
        {
            VisualElement container = element;
            if (!element.ClassListContains(PropertyContainer))
            {
                container = new VisualElement { name = PropertyName(property) };
                container.AddToClassList(PropertyContainer);
                if (element.name == container.name) element.name = null;
                container.Add(element);
            }
            property.ValidationSlot = ValidationSlot(property.Path);
            container.Add(property.ValidationSlot);
            return container;
        }

        static string LabelFor(string displayName, MemberModel member)
        {
            if (member != null)
            {
                if (member.Has<HideLabelAttribute>()) return string.Empty;
                LabelTextAttribute labelText = member.Get<LabelTextAttribute>();
                if (labelText != null) return labelText.Text ?? string.Empty;
            }
            return displayName;
        }

        VisualElement CreateField(InspectorProperty property, IFieldHandler field)
        {
            if (field != null)
            {
                try
                {
                    VisualElement custom = field.CreateField();
                    if (custom != null) return custom;
                }
                catch (Exception exception)
                {
                    return WithError(Fallback(property), Report(property, field.GetType(), exception));
                }
            }

            if (property.SerializedProperty == null) return Fallback(property);
            Type nestedType = TakenOverNestedType(property);
            return nestedType != null ? NestedFoldout(property, TypeModel.For(nestedType)) : DefaultField(property);
        }

        static VisualElement Fallback(InspectorProperty property)
        {
            if (property.SerializedProperty != null) return DefaultField(property);
            return new Label(property.Label) { name = "sm-member:" + property.Path };
        }

        static VisualElement Decorate(InspectorProperty property, VisualElement field)
        {
            var decorators = new List<IDecoratorHandler>();
            foreach (AttributeHandler handler in property.Handlers)
                if (handler is IDecoratorHandler decorator) decorators.Add(decorator);
            if (decorators.Count == 0) return field;

            VisualElement element = field;
            foreach (IDecoratorHandler decorator in decorators.OrderBy(decorator => decorator.Order))
            {
                try
                {
                    element = decorator.Decorate(element) ?? element;
                }
                catch (Exception exception)
                {
                    var container = new VisualElement();
                    container.Add(element);
                    container.Add(new HelpBox(Report(property, decorator.GetType(), exception), HelpBoxMessageType.Error));
                    element = container;
                }
            }
            if (element != field) element.name = PropertyName(property);
            return element;
        }

        static VisualElement WithProblems(InspectorProperty property, VisualElement element)
        {
            List<string> problems = null;
            foreach (AttributeHandler handler in property.Handlers)
                foreach (string problem in handler.Problems)
                    (problems ??= new List<string>()).Add(problem);
            if (problems == null) return element;

            var container = new VisualElement { name = PropertyName(property) };
            container.AddToClassList(PropertyContainer);
            if (element.name == container.name) element.name = null;
            container.Add(element);
            foreach (string problem in problems)
            {
                container.Add(new HelpBox(problem, HelpBoxMessageType.Error));
                string key = $"{property.OwnerType?.FullName}.{property.Name}: {problem}";
                if (reportedProblems.Add(key)) Debug.LogWarning($"SMInspectorPro: {property.OwnerType?.Name}.{property.Name}: {problem}");
            }
            return container;
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

        VisualElement NestedFoldout(InspectorProperty property, TypeModel nestedModel)
        {
            SerializedProperty serializedProperty = property.SerializedProperty;
            var container = new VisualElement { name = FieldName(serializedProperty) };
            AddUnityDecorators(container, property.Member.Info);

            VisualElement content;
            if (string.IsNullOrEmpty(property.Label))
            {
                content = new VisualElement();
                content.AddToClassList("sm-nested");
                container.Add(content);
            }
            else
            {
                var foldout = new Foldout { text = property.Label, tooltip = serializedProperty.tooltip, value = serializedProperty.isExpanded };
                foldout.RegisterValueChangedCallback(change =>
                {
                    if (change.target == foldout) serializedProperty.isExpanded = change.newValue;
                });
                container.Add(foldout);
                content = foldout.contentContainer;
            }

            var children = new List<InspectorProperty>();
            SerializedProperty child = serializedProperty.Copy();
            SerializedProperty end = serializedProperty.GetEndProperty();
            for (bool enterChildren = true; child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end); enterChildren = false)
                children.Add(BuildProperty(child.Copy(), nestedModel, property, property.Targets));
            AddMembers(nestedModel, property, property.Targets, children);
            Arrange(content, nestedModel.Type, children);
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

        static PropertyField DefaultField(InspectorProperty property) =>
            new(property.SerializedProperty, property.Label) { name = FieldName(property.SerializedProperty) };

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

        static string PropertyName(InspectorProperty property) => "sm-property:" + property.Path;

        internal static void ClearReportedErrors()
        {
            reportedErrors.Clear();
            reportedProblems.Clear();
            GroupLayout.ClearReported();
            InspectorValidation.ClearReported();
        }
    }
}
