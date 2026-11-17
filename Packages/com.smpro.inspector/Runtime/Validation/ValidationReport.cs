using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace SMPro.Inspector
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct ValidationIssue
    {
        public ValidationIssue(ValidationSeverity severity, string message, string propertyPath, Object context)
        {
            Severity = severity;
            Message = message;
            PropertyPath = propertyPath;
            Context = context;
        }

        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public string PropertyPath { get; }
        public Object Context { get; }
    }

    public interface ISelfValidator
    {
        void Validate(ValidationReport report);
    }

    public sealed class ValidationReport
    {
        readonly List<ValidationIssue> issues = new();

        public IReadOnlyList<ValidationIssue> Issues => issues;
        public Object Context { get; set; }
        public string PropertyPath { get; set; }
        public bool ApplyFixes { get; set; }

        public bool HasErrors
        {
            get
            {
                foreach (ValidationIssue issue in issues)
                    if (issue.Severity == ValidationSeverity.Error) return true;
                return false;
            }
        }

        public void Error(string message, string propertyPath = null) => Add(ValidationSeverity.Error, message, propertyPath);
        public void Warning(string message, string propertyPath = null) => Add(ValidationSeverity.Warning, message, propertyPath);
        public void Info(string message, string propertyPath = null) => Add(ValidationSeverity.Info, message, propertyPath);

        public void Add(ValidationSeverity severity, string message, string propertyPath = null)
        {
            string path = propertyPath == null ? PropertyPath : string.IsNullOrEmpty(PropertyPath) ? propertyPath : PropertyPath + "." + propertyPath;
            issues.Add(new ValidationIssue(severity, message, path, Context));
        }

        public void Clear() => issues.Clear();
    }
}
