using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class FieldConditionTestComponent : MonoBehaviour
    {
        public bool toggle;
        [ShowIf(nameof(toggle))] public int shownWhenOn;
        [ShowIf("missingMember")] public int broken;
    }
}
