using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.EditorTools
{
    internal sealed class InspectorValidation
    {
        sealed class Entry
        {
            public InspectorProperty Property;
            public IValidatorHandler[] Validators;
        }

        static readonly HashSet<string> reportedFailures = new();

        readonly SerializedObject serializedObject;
        readonly List<Entry> entries = new();
        readonly List<InspectorProperty> selfValidating = new();
        readonly Dictionary<string, ValidationSlot> slots = new(StringComparer.Ordinal);
        readonly ValidationSlot topSlot;
        readonly ValidationReport liveReport = new() { ApplyFixes = true };

        public InspectorValidation(SerializedObject serializedObject, IEnumerable<InspectorProperty> properties, VisualElement topSlot)
        {
            this.serializedObject = serializedObject;
            if (topSlot != null) this.topSlot = new ValidationSlot(topSlot);
            foreach (InspectorProperty property in properties)
            {
                var validators = new List<IValidatorHandler>();
                foreach (AttributeHandler handler in property.Handlers)
                    if (handler is IValidatorHandler validator) validators.Add(validator);
                validators.AddRange(property.ExtraValidators);
                if (validators.Count > 0) entries.Add(new Entry { Property = property, Validators = validators.ToArray() });
                if (property.SerializedProperty != null && IsNestedSelfValidator(property.Member?.ValueType)) selfValidating.Add(property);
                if (property.ValidationSlot != null) slots[property.Path] = new ValidationSlot(property.ValidationSlot);
            }
        }

        Object Target => serializedObject.targetObject;
        bool RootValidatesItself => Target is ISelfValidator;
        public bool IsEmpty => entries.Count == 0 && selfValidating.Count == 0 && !RootValidatesItself;
        public IReadOnlyList<ValidationIssue> Issues => liveReport.Issues;

        public void Refresh()
        {
            if (IsEmpty) return;
            liveReport.Clear();
            Run(liveReport);
            Show(liveReport);
        }

        public void Run(ValidationReport report)
        {
            serializedObject.UpdateIfRequiredOrScript();
            report.Context = Target;
            foreach (Entry entry in entries)
            {
                if (!IsShown(entry.Property)) continue;
                report.PropertyPath = entry.Property.Path;
                foreach (IValidatorHandler validator in entry.Validators)
                    Guard(report, validator.GetType().Name, () => validator.Validate(report));
            }
            if (Target is ISelfValidator self)
            {
                report.PropertyPath = null;
                Guard(report, Target.GetType().Name, () => self.Validate(report));
            }
            foreach (InspectorProperty property in selfValidating)
            {
                if (!IsShown(property) || property.GetValue() is not ISelfValidator nested) continue;
                report.PropertyPath = property.Path;
                Guard(report, nested.GetType().Name, () => nested.Validate(report));
            }
            report.PropertyPath = null;
        }

        internal static bool IsNestedSelfValidator(Type type) =>
            type != null && typeof(ISelfValidator).IsAssignableFrom(type) && !typeof(Object).IsAssignableFrom(type);

        static bool IsShown(InspectorProperty property)
        {
            for (InspectorProperty current = property; current != null; current = current.Parent)
                if (!current.IsVisible) return false;
            return true;
        }

        static void Guard(ValidationReport report, string source, Action validate)
        {
            try
            {
                validate();
            }
            catch (Exception exception)
            {
                report.Error($"{source} failed: {exception.Message}");
                if (reportedFailures.Add(source + exception.Message)) Debug.LogException(exception);
            }
        }

        internal static void ClearReported() => reportedFailures.Clear();

        void Show(ValidationReport report)
        {
            foreach (ValidationSlot slot in slots.Values) slot.Begin();
            topSlot?.Begin();
            foreach (ValidationIssue issue in report.Issues) SlotFor(issue.PropertyPath)?.Add(issue);
            foreach (ValidationSlot slot in slots.Values) slot.End();
            topSlot?.End();
        }

        ValidationSlot SlotFor(string path)
        {
            while (!string.IsNullOrEmpty(path))
            {
                if (slots.TryGetValue(path, out ValidationSlot slot)) return slot;
                int dot = path.LastIndexOf('.');
                path = dot < 0 ? null : path.Substring(0, dot);
            }
            return topSlot;
        }

        sealed class ValidationSlot
        {
            readonly VisualElement element;
            readonly List<(ValidationSeverity severity, string message)> pending = new();
            readonly List<(ValidationSeverity severity, string message)> shown = new();

            public ValidationSlot(VisualElement element) => this.element = element;

            public void Begin() => pending.Clear();

            public void Add(ValidationIssue issue) => pending.Add((issue.Severity, issue.Message));

            public void End()
            {
                if (Same()) return;
                shown.Clear();
                shown.AddRange(pending);
                element.Clear();
                foreach ((ValidationSeverity severity, string message) in shown)
                    element.Add(new HelpBox(message, ToHelpBoxType(severity)));
            }

            bool Same()
            {
                if (pending.Count != shown.Count) return false;
                for (int i = 0; i < pending.Count; i++)
                    if (pending[i] != shown[i]) return false;
                return true;
            }

            static HelpBoxMessageType ToHelpBoxType(ValidationSeverity severity) => severity switch
            {
                ValidationSeverity.Error => HelpBoxMessageType.Error,
                ValidationSeverity.Warning => HelpBoxMessageType.Warning,
                _ => HelpBoxMessageType.Info
            };
        }
    }
}
