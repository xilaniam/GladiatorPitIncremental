using UnityEditor;

namespace SMPro.Inspector.EditorTools
{
    [UnityEditor.FilePath("ProjectSettings/SMInspectorSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
    public sealed class SMInspectorSettings : ScriptableSingleton<SMInspectorSettings>
    {
        public bool validateBeforeBuild;
        public bool failBuildOnErrors = true;

        public void Save() => Save(true);
    }
}
