using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class SelfCheckingTestAsset : ScriptableObject, ISelfValidator
    {
        public void Validate(ValidationReport report) => report.Error("Asset problem");
    }
}
