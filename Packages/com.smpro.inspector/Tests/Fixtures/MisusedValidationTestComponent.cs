using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class MisusedValidationTestComponent : MonoBehaviour
    {
        [Required] public int notAReference;
        [MinValue(0)] public string notANumber;
        [ValidateInput(nameof(Explodes))] public int exploding;

        bool Explodes(int value) => throw new System.InvalidOperationException("validator boom");
    }
}
