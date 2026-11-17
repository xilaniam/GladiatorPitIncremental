using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class ThrowingTestComponent : MonoBehaviour
    {
        [TestThrowing] public int broken;
        [TestThrowingDecorator] public int brokenDecorator;
        public int healthy;
    }
}
