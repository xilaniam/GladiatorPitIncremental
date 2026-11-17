using System.Linq;
using System.Text;
using SMPro.SkillTree;
using TMPro;
using UnityEngine;

namespace SMPro.SkillTree.Samples
{
    public class StatPanel : MonoBehaviour
    {
        [SerializeField] SkillTreeController controller;
        [SerializeField] TMP_Text text;

        void OnEnable()
        {
            if (controller == null || controller.Definition == null) return;
            controller.Stats.StatChanged += OnStatChanged;
            Redraw();
        }

        void OnDisable()
        {
            if (controller != null && controller.Definition != null) controller.Stats.StatChanged -= OnStatChanged;
        }

        void OnStatChanged(Stat stat, float oldValue, float newValue) => Redraw();

        void Redraw()
        {
            var lines = new StringBuilder("<b>Stats</b>\n");
            foreach (Stat stat in controller.Stats.All.OrderBy(s => s.Definition.DisplayName))
            {
                string value = stat.Definition.format == StatFormat.Percent ? $"{stat.Value * 100f:0.#}%" : $"{stat.Value:0.##}";
                lines.AppendLine($"{stat.Definition.DisplayName}: {value}");
            }
            text.text = lines.ToString();
        }
    }
}
