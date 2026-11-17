using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SMPro.Inspector.EditorTools
{
    internal sealed class BuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            SMInspectorSettings settings = SMInspectorSettings.instance;
            if (!settings.validateBeforeBuild) return;

            ValidationResult result = SMValidator.ValidateProject(ValidationOptions.ForBuild);
            foreach (ValidationRecord record in result.Records) SMValidator.Log(record);
            if (ShouldFail(result, settings))
                throw new BuildFailedException($"SMInspectorPro found {result.ErrorCount} validation error(s). Fix them, or turn off 'Fail the build on errors' in Project Settings > SMPro > Inspector.");
        }

        internal static bool ShouldFail(ValidationResult result, SMInspectorSettings settings) => settings.failBuildOnErrors && result.ErrorCount > 0;
    }
}
