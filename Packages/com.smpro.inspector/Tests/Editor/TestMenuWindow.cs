using System.Collections.Generic;
using SMPro.Inspector.EditorTools;
using UnityEngine;

namespace SMPro.Inspector.Tests
{
    public class TestMenuWindow : SMMenuWindow
    {
        public static readonly List<(string path, Object target)> Items = new();

        protected override void BuildMenu(MenuTree tree)
        {
            foreach ((string path, Object target) in Items) tree.Add(path, target);
        }
    }
}
