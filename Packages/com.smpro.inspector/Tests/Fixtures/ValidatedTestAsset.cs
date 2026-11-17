using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class ValidatedTestAsset : ScriptableObject
    {
        [Required] public Object reference;
        [MinValue(0)] public int count;
    }
}
