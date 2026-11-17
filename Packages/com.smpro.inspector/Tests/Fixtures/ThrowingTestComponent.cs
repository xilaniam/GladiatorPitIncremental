using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class ThrowingTestComponent : MonoBehaviour
    {
        [TestThrowing] public int broken;
        public int healthy;
    }
}
