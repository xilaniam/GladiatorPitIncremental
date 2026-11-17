using UnityEditor;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(ReadOnlyAttribute))]
    public sealed class ReadOnlyHandler : StateHandler<ReadOnlyAttribute>
    {
        public override void Apply(ref PropertyState state) => state.Enabled = false;
    }

    [AttributeHandler(typeof(ShowInPlayModeAttribute))]
    public sealed class ShowInPlayModeHandler : StateHandler<ShowInPlayModeAttribute>
    {
        public override void Apply(ref PropertyState state)
        {
            if (!EditorApplication.isPlaying) state.Visible = false;
        }
    }

    [AttributeHandler(typeof(HideInPlayModeAttribute))]
    public sealed class HideInPlayModeHandler : StateHandler<HideInPlayModeAttribute>
    {
        public override void Apply(ref PropertyState state)
        {
            if (EditorApplication.isPlaying) state.Visible = false;
        }
    }
}
