using IplanHEE.Admin.Domain.ApprovalTree;
using IplanHEE.Admin.Domain.UnitTests.SeedWork;
using System;
using System.Reflection;

namespace IplanHEE.Admin.Domain.UnitTests.ApprovalTree
{
    public class TreeTestBase : TestBase
    {
        protected class TreeTestDataOptions
        {
            internal TreeId TreeId { get; set; }
            internal string TreeName { get; set; }
            internal SubTreeId SubTreeId { get; set; }
            internal string SubTreeName { get; set; }
            internal SubTreeId ParentSubTreeId { get; set; }
            internal bool IsDataNodeEnable { get; set; }
            internal bool IsLeafSubTree { get; set; }

        }
        protected class TreeTestData
        {
            public TreeTestData(Tree tree)
            {
                Tree = tree;
            }
            internal Tree Tree { get; }

        }

        protected TreeTestData CreateTreeTestData(TreeTestDataOptions options)
        {
            var tree = (Tree)Activator.CreateInstance(typeof(Tree), true);
            typeof(Tree).GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(tree, options.TreeId);
            typeof(Tree).GetField("_name", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(tree, options.TreeName);

            return new TreeTestData(tree);
        }
        protected TreeTestData AddSubTreeWithTreeTestData(TreeTestDataOptions options, TreeTestData treeTestData)
        {

            var subTree = (SubTree)Activator
                .CreateInstance(typeof(SubTree), true);

            typeof(SubTree)
                .GetProperty("Id", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(subTree, options.SubTreeId);

            typeof(SubTree)
                    .GetField("_isLeafSubTree", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(subTree, options.IsLeafSubTree);
            typeof(SubTree)
                .GetField("_treeId", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(subTree, options.TreeId);

            typeof(SubTree)
                .GetProperty("Name", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(subTree, options.SubTreeName);
            
            typeof(SubTree)
                .GetProperty("IsDataNodeEnable", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(subTree, options.IsDataNodeEnable);


            typeof(SubTree)
                .GetProperty("ParentSubTreeId", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(subTree, options.ParentSubTreeId);

            var subTreeList = typeof(Tree)
                    .GetField("_subTrees", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(treeTestData.Tree);
            subTreeList?.GetType()
                .GetMethod("Add")
                ?.Invoke(subTreeList, new[] { subTree });

            typeof(Tree)
                .GetField("_subTrees", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(treeTestData.Tree, subTreeList);

            return treeTestData;
        }
    }
}
