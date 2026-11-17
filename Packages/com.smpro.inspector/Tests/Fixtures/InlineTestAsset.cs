using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class InlineTestAsset : ScriptableObject
    {
        [Range(0, 10)] public int level = 3;
        [InlineEditor] public InlineTestAsset next;
    }
}
