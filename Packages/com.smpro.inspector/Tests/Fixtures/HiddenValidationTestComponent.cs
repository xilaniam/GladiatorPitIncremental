using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class HiddenValidationTestComponent : MonoBehaviour
    {
        public bool needsTarget;
        [ShowIf(nameof(needsTarget)), Required] public GameObject target;
        public SelfCheckingTestAsset linked;
    }
}
