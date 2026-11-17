using System.Collections.Generic;
using SMPro.Inspector;
using UnityEngine;

namespace SMPro.Inspector.Samples
{
    public enum Element
    {
        Fire,
        Water,
        Earth,
        Air
    }

    [CreateAssetMenu(menuName = "SMPro/Showcase/Enemy Profile")]
    public class EnemyProfile : ScriptableObject, ISelfValidator
    {
        [TabGroup("Tabs", "Stats"), HorizontalGroup("Tabs/Stats/Top", 72f), PreviewField(64f), HideLabel]
        public Sprite icon;

        [TabGroup("Tabs", "Stats"), VerticalGroup("Tabs/Stats/Top/Info"), Required]
        public string displayName = "Enemy";

        [TabGroup("Tabs", "Stats"), VerticalGroup("Tabs/Stats/Top/Info"), ValueDropdown(nameof(Rarities))]
        public string rarity = "Common";

        [TabGroup("Tabs", "Stats"), MinValue(1)]
        public int maxHealth = 100;

        [TabGroup("Tabs", "Stats"), ProgressBar(MaxMember = nameof(maxHealth))]
        public int startingHealth = 100;

        [TabGroup("Tabs", "Combat"), EnumToggleButtons]
        public Element element;

        [TabGroup("Tabs", "Combat"), MinMaxSlider(0f, 50f)]
        public Vector2 damageRange = new(5f, 12f);

        [TabGroup("Tabs", "Combat"), Required, AssetsOnly]
        public GameObject prefab;

        [TabGroup("Tabs", "Loot"), MinValue(0), Suffix("coins")]
        public int coins = 5;

        [TabGroup("Tabs", "Loot"), TextArea]
        public string notes;

        static IEnumerable<string> Rarities => new[] { "Common", "Rare", "Epic", "Legendary" };

        public void Validate(ValidationReport report)
        {
            if (startingHealth > maxHealth) report.Error("Starting health is above max health", nameof(startingHealth));
            if (damageRange.y <= 0f) report.Warning("This enemy never deals damage", nameof(damageRange));
        }
    }
}
