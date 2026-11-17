using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.SkillTree.EditorTools
{
    internal class SkillNodeElement : Node
    {
        static readonly Color ErrorColor = new(0.9f, 0.25f, 0.25f);
        static readonly Color WarningColor = new(0.95f, 0.75f, 0.2f);

        readonly Image icon;
        readonly Label summary;

        public SkillNodeDefinition Definition { get; private set; }
        public Port Input { get; }
        public Port Output { get; }

        public SkillNodeElement(SkillNodeDefinition definition)
        {
            Definition = definition;

            icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.style.width = 28;
            icon.style.height = 28;
            icon.style.marginLeft = 6;
            icon.style.alignSelf = Align.Center;
            titleContainer.Insert(0, icon);

            Input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            Input.portName = "Requires";
            inputContainer.Add(Input);

            Output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            Output.portName = "Unlocks";
            outputContainer.Add(Output);

            summary = new Label();
            summary.style.whiteSpace = WhiteSpace.Normal;
            summary.style.paddingLeft = 6;
            summary.style.paddingRight = 6;
            summary.style.paddingTop = 4;
            summary.style.paddingBottom = 4;
            summary.style.maxWidth = 240;
            extensionContainer.Add(summary);
            extensionContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.9f);

            RefreshExpandedState();
            RefreshPorts();
        }

        public void Refresh(SkillTreeDefinition tree, IssueSeverity? worstIssue)
        {
            title = Definition.DisplayName;
            icon.image = Definition.icon != null ? Definition.icon.texture : null;
            icon.style.display = Definition.icon != null ? DisplayStyle.Flex : DisplayStyle.None;

            string effects = string.Join("\n", Definition.effects
                .Where(e => e != null)
                .Select(e => e.Describe(tree, 1))
                .Where(line => !string.IsNullOrEmpty(line))
                .Take(3));
            string reveal = Definition.reveal == RevealRule.WhenParentStarted ? "" : $" · {Definition.reveal}";
            summary.text = $"Max {Definition.maxLevel} · {CostSummary(Definition.cost)}{reveal}" + (effects.Length > 0 ? "\n" + effects : "");

            Color border = worstIssue == IssueSeverity.Error ? ErrorColor
                : worstIssue == IssueSeverity.Warning ? WarningColor
                : Color.clear;
            float width = worstIssue == null ? 0 : 2;
            mainContainer.style.borderTopColor = mainContainer.style.borderBottomColor =
                mainContainer.style.borderLeftColor = mainContainer.style.borderRightColor = border;
            mainContainer.style.borderTopWidth = mainContainer.style.borderBottomWidth =
                mainContainer.style.borderLeftWidth = mainContainer.style.borderRightWidth = width;
        }

        static string CostSummary(CostCurve cost) => cost switch
        {
            null => "free",
            FlatCost flat => $"cost {flat.amount}",
            LinearCost linear => $"cost {linear.baseCost} + {linear.step}/lv",
            ExponentialCost exponential => $"cost {exponential.baseCost} x {exponential.growth:0.##}^lv",
            TableCost table => $"cost table ({table.costs?.Length ?? 0})",
            _ => cost.GetType().Name
        };
    }
}
