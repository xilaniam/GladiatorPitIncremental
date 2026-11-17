using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEngine;

namespace SMPro.Inspector.Samples.EditorTools
{
    public class EnemyDatabaseWindow : SMMenuWindow
    {
        [MenuItem("Window/SMPro/Showcase/Enemy Database")]
        static void Open() => GetWindow<EnemyDatabaseWindow>().titleContent = new GUIContent("Enemy Database");

        protected override void BuildMenu(MenuTree tree)
        {
            tree.AddAllAssetsOf<EnemyProfile>("Enemies");
        }
    }
}
