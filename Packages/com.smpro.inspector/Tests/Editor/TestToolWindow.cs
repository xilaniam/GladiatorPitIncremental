using SMPro.Inspector.EditorTools;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class TestToolWindow : SMEditorWindow
    {
        [BoxGroup("Settings")] public int count;
        [BoxGroup("Settings"), Required] public Object reference;

        [Button] public void Increment() => count++;
    }
}
