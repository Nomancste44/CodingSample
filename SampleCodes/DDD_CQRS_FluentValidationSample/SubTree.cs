using System;
using IplanHEE.Admin.Domain.ApprovalTree.Rules;
using IplanHEE.BuildingBlocks.Domain;
using System.Collections.Generic;
using System.Linq;
using IplanHEE.BuildingBlocks.Domain.SharedKernel;

namespace IplanHEE.Admin.Domain.ApprovalTree
{
    public class SubTree : Entity
    {
        private readonly TreeId _treeId;
        private readonly string _label;
        private readonly bool _isLeafSubTree;
        internal bool IsPushDown { get; }
        internal SubTreeId Id { get; private set; }
        internal int SubTreeSeedValue { get; private set; }
        internal SubTreeId ParentSubTreeId { get; private set; }
        internal List<Node> NodeList { get; }
        internal string Name { get; private set; }
        internal bool IsDataNodeEnable { get; private set; }

        private SubTree()
        {
            NodeList = new List<Node>();
        }

        internal void AddNodeAt(int index, Node node)
        {
            var nodes = NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex).ToList();
            this.CheckRule(new IsIndexOutOfBoundForAddOperationRule<Node>(index, nodes));
            this.CheckRule(new AnAuthorNodeMustBeFollowedByADataNodeRule(nodes, node, index));
            this.CheckRule(new ADataNodeCannotFollowAnotherDataNodeConsecutivelyRule(nodes, node, index));
            this.CheckRule(new ADataNodeNameCannotBeDuplicatedInASubTreeRule(nodes, node));

            this.NodeList.RemoveAll(n => !n.IsPublish);
            nodes.Insert(index, node);
            this.NodeList.AddRange(nodes);
        }

        internal void RemoveNodeAt(int index)
        {
            var nodes = NodeList.
                Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex).ToList();
            this.CheckRule(new IsIndexOutOfBoundForDeleteOperationRule<Node>(index, nodes));
            this.CheckRule(new CannotDeleteLeafDataNodeRule(nodes, index));
            this.CheckRule(new DeletingAnAuthorNodeCannotLeftTwoDataNodeConsecutivelyRule(index, nodes));
            this.CheckRule(new DeletingADataNodeCannotOrphanAuthorNodesRule(index, nodes));

            this.NodeList.Remove(nodes[index]);
        }

        internal void UpdateChildSubTreeSeedValue(List<SubTree> subTrees)
        {
            if (_isLeafSubTree) return;

            var childSubTrees = subTrees
                .Where(x => x.ParentSubTreeId == this.Id)
                .ToList();
            childSubTrees.ForEach(x =>
            {
                x.SubTreeSeedValue = this.NodeList
                    .Where(n => !n.IsPublish && n.IsAuthorNode)
                    .OrderBy(n => n.NodeIndex)
                    ?.Count() ?? 0;
            });
        }


        internal void PublishEditedNodes()
        {
            this.NodeList.RemoveAll(n => n.IsPublish);
            var publishingNodes = NodeList.Where(n => !n.IsPublish)
                .ToList()
                .CloneObjectSerializable();
            publishingNodes.ForEach(pn => pn.PublishNode());
            this.NodeList.AddRange(publishingNodes);
        }

        internal void ResetEditedNodes()
        {
            var resettingNodes = NodeList
                .Where(n => n.IsPublish)
                .ToList()
                .CloneObjectSerializable();

            resettingNodes.ForEach(rn => rn.ResetNode());

            this.NodeList.RemoveAll(n => !n.IsPublish);
            this.NodeList.AddRange(resettingNodes);
        }

    }
}
