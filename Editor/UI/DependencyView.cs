using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class DependencyView : TreeView
    {
        private readonly Dictionary<int, DependencyNode> m_NodeDictionary = new Dictionary<int, DependencyNode>();
        private readonly Action<Location> m_OnDoubleClick;
        private DependencyNode m_Root;

        public DependencyView(TreeViewState treeViewState, Action<Location> onDoubleClick)
            : base(treeViewState)
        {
            m_OnDoubleClick = onDoubleClick;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Hidden Root"};
            var allItems = new List<TreeViewItem>();

            if (m_Root != null)
            {
                m_NodeDictionary.Clear();
                AddNode(allItems, m_Root, 0);
            }

            // Utility method that initializes the TreeViewItem.children and -parent for all items.
            SetupParentsAndChildrenFromDepths(root, allItems);

            // Return root of the tree
            return root;
        }

        public void SetRoot(DependencyNode root)
        {
            if (m_Root != root)
            {
                m_Root = root;

                Reload();
            }
        }

        private void AddNode(List<TreeViewItem> items, DependencyNode node, int depth)
        {
            var id = items.Count;
            items.Add(new TreeViewItem {id = id, depth = depth, displayName = node.GetPrettyName()}); // TODO add assembly name

            m_NodeDictionary.Add(id, node);

            // if the tree is too deep, serialization will exceed the 7 levels limit.
            if (!node.HasValidChildren())
                items.Add(new TreeViewItem {id = id + 1, depth = depth + 1, displayName = "<Serialization Limit>"});
            else
                for (int i = 0; i < node.GetNumChildren(); i++)
                {
                    AddNode(items, node.GetChild(i), depth + 1);
                }
        }

        protected override void DoubleClickedItem(int id)
        {
            if (m_NodeDictionary.ContainsKey(id))
            {
                var node = m_NodeDictionary[id];
                if (node.location != null)
                    m_OnDoubleClick(node.location);
            }
        }
    }
}
