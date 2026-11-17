using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class AttributedTestAsset : ScriptableObject
    {
        [TestField] public int marked;
        public string title;
    }
}
