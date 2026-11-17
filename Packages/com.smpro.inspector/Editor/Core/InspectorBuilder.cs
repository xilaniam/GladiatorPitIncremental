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
        readonly List<IRefreshable> extras = new();
        readonly List<StructureKey> structure = new();
        readonly Action requestRebuild;

        InspectorBuilder(Action requestRebuild) => this.requestRebuild = requestRebuild;

        internal readonly struct Result
        {
            public Result(InspectorRefresher refresher, List<StructureKey> structure)
            {
                Refresher = refresher;
                Structure = structure;
            }

            public InspectorRefresher Refresher { get; }
            public List<StructureKey> Structure { get; }
        }

        public static VisualElement Build(SerializedObject serializedObject, bool showScript = true)
        {
            VisualElement root = InspectorView.Create(serializedObject, showScript);
            AddStyles(root);
            return root;
        }

        internal static Result BuildContent(VisualElement root, SerializedObject serializedObject, Action requestRebuild, bool showScript = true)
        {
            var builder = new InspectorBuilder(requestRebuild);
            TypeModel model = TypeModel.For(serializedObject.targetObject.GetType());
            Object[] targets = serializedObject.targetObjects;
            var properties = new List<InspectorProperty>();

            VisualElement topSlot = null;
            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                SerializedProperty property = iterator.Copy();
                if (property.propertyPath == ScriptPath)
                {
                    if (showScript) root.Add(ScriptField(property));
                }
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
            var refresher = new InspectorRefresher(serializedObject, builder.built, builder.collapsibles, builder.extras, validation);
            return new Result(refresher, builder.structure);
        }

        static VisualElement ValidationSlot(string path)
        {
            var slot = new VisualElement { name = "sm-validation:" + path };
            slot.AddToClassList("sm-validation");
            return slot;
        }

        internal static InspectorRefresher RefresherOf(VisualElement root) => InspectorView.Of(root)?.Refresher;

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

        InspectorProperty BuildProperty(SerializedProperty serializedProperty, TypeModel ownerModel, InspectorProperty parent, Object[] targets, string label = null)
        {
            MemberModel member = ownerModel?.Find(serializedProperty.name);
            var property = new InspectorProperty(serializedProperty, member, ownerModel?.Type, parent, targets)
            {
                Label = label ?? LabelFor(serializedProperty.displayName, member)
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
            property.RequestRebuild = requestRebuild;
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
            if (property.ExtraValidators.Count > 0) return true;
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
            if (property.SerializedProperty.propertyType == SerializedPropertyType.ManagedReference) structure.Add(StructureKey.Of(property.SerializedProperty));
            Type dictionaryType = TakenOverDictionaryType(property);
            if (dictionaryType != null) return BuildDictionary(property);
            Type elementType = TakenOverElementType(property);
            if (elementType != null) return BuildList(property, elementType);
            Type nestedType = TakenOverNestedType(property);
            return nestedType != null ? NestedFoldout(property, TypeModel.For(nestedType)) : DefaultField(property);
        }

        static Type TakenOverElementType(InspectorProperty property)
        {
            SerializedProperty serializedProperty = property.SerializedProperty;
            MemberModel member = property.Member;
            if (member == null || !serializedProperty.isArray || serializedProperty.propertyType != SerializedPropertyType.Generic) return null;
            Type elementType = ElementTypeOf(member.ValueType);
            if (elementType == null) return null;
            foreach (PropertyAttribute attribute in member.Info.GetCustomAttributes<PropertyAttribute>(true))
                if (PropertyDrawerLookup.HasDrawerFor(attribute.GetType())) return null;
            if (member.Has<ListSettingsAttribute>() || member.Has<TableListAttribute>()) return elementType;
            return IsComplex(elementType) && !member.Info.IsDefined(typeof(SerializeReference), true) &&
                   TypeModel.For(elementType).UsesInspectorAttributes && !PropertyDrawerLookup.HasDrawerFor(elementType)
                ? elementType
                : null;
        }

        static Type TakenOverDictionaryType(InspectorProperty property)
        {
            MemberModel member = property.Member;
            if (member == null || property.SerializedProperty.propertyType != SerializedPropertyType.Generic) return null;
            if (PropertyDrawerLookup.HasDrawerFor(member.ValueType)) return null;
            for (Type current = member.ValueType; current != null; current = current.BaseType)
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(SerializableDictionary<,>)) return current;
            return null;
        }

        VisualElement BuildDictionary(InspectorProperty property)
        {
            SerializedProperty entries = property.SerializedProperty.FindPropertyRelative("entries");
            TypeModel dictionaryModel = TypeModel.For(property.Member.ValueType);
            MemberModel entriesMember = dictionaryModel.Find("entries");
            if (entries == null || entriesMember == null) return DefaultField(property);
            structure.Add(StructureKey.Of(entries));

            var list = new InspectorProperty(entries, entriesMember, dictionaryModel.Type, property, property.Targets) { Label = property.Label };
            TypeModel entryModel = TypeModel.For(ElementTypeOf(entriesMember.ValueType));
            var rows = new List<IReadOnlyList<VisualElement>>(entries.arraySize);
            for (int i = 0; i < entries.arraySize; i++)
            {
                var element = new InspectorProperty(entries.GetArrayElementAtIndex(i), null, null, list, property.Targets)
                {
                    ElementIndex = i,
                    Label = "Entry " + i,
                    Element = new VisualElement()
                };
                built.Add(element);
                rows.Add(new[]
                {
                    BuildProperty(element.SerializedProperty.FindPropertyRelative("key"), entryModel, element, element.Targets, string.Empty).Element,
                    BuildProperty(element.SerializedProperty.FindPropertyRelative("value"), entryModel, element, element.Targets, string.Empty).Element
                });
            }
            property.ExtraValidators.Add(new DictionaryValidator(property));
            return new TableDrawer("sm-dictionary:" + property.Path, property.Label, entries, false, true, new[] { "Key", "Value" }, rows, requestRebuild).Element;
        }

        sealed class DictionaryValidator : IValidatorHandler
        {
            readonly InspectorProperty property;

            public DictionaryValidator(InspectorProperty property) => this.property = property;

            public void Validate(ValidationReport report)
            {
                if (property.GetValue() is not ISerializableDictionary dictionary) return;
                foreach (int index in dictionary.FindMissingKeys()) report.Error($"Entry {index} has no key");
                foreach ((int first, int duplicate) in dictionary.FindDuplicateKeys())
                    report.Error($"Duplicate key '{dictionary.KeyAt(duplicate)}' in entries {first} and {duplicate}; only the first is used");
            }
        }

        static Type ElementTypeOf(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0] : null;
        }

        static bool IsComplex(Type type) =>
            !type.IsPrimitive && !type.IsEnum && type != typeof(string) && !typeof(Object).IsAssignableFrom(type) &&
            type.Namespace?.StartsWith("UnityEngine", StringComparison.Ordinal) != true;

        VisualElement BuildList(InspectorProperty property, Type elementType)
        {
            SerializedProperty array = property.SerializedProperty;
            structure.Add(StructureKey.Of(array));
            TypeModel elementModel = TypeModel.For(elementType);
            MemberModel member = property.Member;
            bool complex = IsComplex(elementType) && !member.Info.IsDefined(typeof(SerializeReference), true);

            var elements = new List<InspectorProperty>(array.arraySize);
            for (int i = 0; i < array.arraySize; i++)
            {
                var element = new InspectorProperty(array.GetArrayElementAtIndex(i), null, null, property, property.Targets)
                {
                    ElementIndex = i,
                    Label = "Element " + i
                };
                built.Add(element);
                elements.Add(element);
            }

            TableListAttribute table = member.Get<TableListAttribute>();
            if (table != null)
            {
                if (complex) return BuildTable(property, table, elementModel, elements);
                property.AddBuilderProblem("TableList: only works on lists of serializable classes or structs");
            }

            ListSettingsAttribute settings = member.Get<ListSettingsAttribute>() ?? new ListSettingsAttribute();
            Getter<object> elementLabel = default;
            if (complex && !string.IsNullOrEmpty(settings.ElementLabel))
            {
                elementLabel = MemberResolver.Value<object>(elementType, settings.ElementLabel);
                if (!elementLabel.IsValid) property.AddBuilderProblem($"ListSettings: {elementLabel.Error}");
            }

            var items = new List<VisualElement>(elements.Count);
            foreach (InspectorProperty element in elements) items.Add(BuildElement(element, elementModel, complex, elementLabel));
            return new ListDrawer(property, settings, items, requestRebuild).Element;
        }

        VisualElement BuildElement(InspectorProperty element, TypeModel elementModel, bool complex, Getter<object> label)
        {
            SerializedProperty serializedProperty = element.SerializedProperty;
            if (!complex || serializedProperty.propertyType != SerializedPropertyType.Generic)
            {
                element.Element = new PropertyField(serializedProperty, element.Label) { name = FieldName(serializedProperty) };
                return element.Element;
            }

            element.Element = NestedContent(element, elementModel, null, out Foldout foldout);
            if (label.IsValid && foldout != null) extras.Add(new ElementLabel(element, label, foldout));
            return element.Element;
        }

        VisualElement BuildTable(InspectorProperty property, TableListAttribute table, TypeModel elementModel, List<InspectorProperty> elements)
        {
            var columns = new List<MemberModel>();
            foreach (MemberModel member in elementModel.Members)
                if (member.Kind == MemberKind.SerializedField && !member.Info.IsDefined(typeof(HideInInspector), true)) columns.Add(member);
            List<string> titles = columns.ConvertAll(column => LabelFor(ObjectNames.NicifyVariableName(column.Name), column));

            var rows = new List<IReadOnlyList<VisualElement>>(elements.Count);
            foreach (InspectorProperty element in elements)
            {
                var cells = new List<VisualElement>(columns.Count);
                foreach (MemberModel column in columns)
                {
                    SerializedProperty cell = element.SerializedProperty.FindPropertyRelative(column.Name);
                    cells.Add(cell == null ? new VisualElement() : BuildProperty(cell, elementModel, element, element.Targets, string.Empty).Element);
                }
                rows.Add(cells);
                element.Element = new VisualElement();
            }
            return new TableDrawer("sm-table:" + property.Path, property.Label, property.SerializedProperty, table.ShowIndex, !table.IsReadOnly, titles, rows, requestRebuild).Element;
        }

        sealed class ElementLabel : IRefreshable
        {
            readonly InspectorProperty element;
            readonly Getter<object> getter;
            readonly Foldout foldout;
            string shown;

            public ElementLabel(InspectorProperty element, Getter<object> getter, Foldout foldout)
            {
                this.element = element;
                this.getter = getter;
                this.foldout = foldout;
            }

            public bool NeedsPolling => false;

            public void Refresh()
            {
                object value = element.GetValue();
                string text = value == null ? null : getter.Get(value)?.ToString();
                if (string.IsNullOrEmpty(text)) text = element.Label;
                if (text == shown) return;
                shown = text;
                foldout.text = text;
            }
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
            foreach (string problem in property.BuilderProblems)
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
            if (member == null) return null;
            if (serializedProperty.propertyType == SerializedPropertyType.ManagedReference)
            {
                Type current = serializedProperty.managedReferenceValue?.GetType();
                return current != null && TypeModel.For(current).UsesInspectorAttributes && !PropertyDrawerLookup.HasDrawerFor(current) ? current : null;
            }
            if (serializedProperty.propertyType != SerializedPropertyType.Generic || serializedProperty.isArray) return null;
            Type type = member.ValueType;
            if (!TypeModel.For(type).UsesInspectorAttributes || PropertyDrawerLookup.HasDrawerFor(type)) return null;
            foreach (PropertyAttribute attribute in member.Info.GetCustomAttributes<PropertyAttribute>(true))
                if (PropertyDrawerLookup.HasDrawerFor(attribute.GetType())) return null;
            return type;
        }

        VisualElement NestedFoldout(InspectorProperty property, TypeModel nestedModel) => NestedContent(property, nestedModel, property.Member.Info, out _);

        VisualElement NestedContent(InspectorProperty property, TypeModel nestedModel, MemberInfo decorated, out Foldout foldout)
        {
            SerializedProperty serializedProperty = property.SerializedProperty;
            var container = new VisualElement { name = FieldName(serializedProperty) };
            if (decorated != null) AddUnityDecorators(container, decorated);

            VisualElement content;
            if (string.IsNullOrEmpty(property.Label))
            {
                foldout = null;
                content = new VisualElement();
                content.AddToClassList("sm-nested");
                container.Add(content);
            }
            else
            {
                var expander = new Foldout { text = property.Label, tooltip = serializedProperty.tooltip, value = serializedProperty.isExpanded };
                expander.RegisterValueChangedCallback(change =>
                {
                    if (change.target == expander) serializedProperty.isExpanded = change.newValue;
                });
                container.Add(expander);
                content = expander.contentContainer;
                foldout = expander;
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
