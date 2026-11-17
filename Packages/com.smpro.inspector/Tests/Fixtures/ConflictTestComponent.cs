using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class ConflictTestComponent : MonoBehaviour
    {
        [BoxGroup("A")] public int boxed;
        [FoldoutGroup("A")] public int alsoBoxed;
    }
}
