using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class MisusedFieldTestComponent : MonoBehaviour
    {
        [ValueDropdown("missing")] public int dropdownWithoutSource;
        [EnumToggleButtons] public int notAnEnum;
        [ProgressBar] public string notANumber;
        [MinMaxSlider] public float notAVector;
        [PreviewField] public int notAnObject;
        [FilePath] public int notAString;
    }
}
