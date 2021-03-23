using IplanHEE.Admin.Domain.ApprovalTree.Rules;
using IplanHEE.Admin.Domain.SharedLine;
using IplanHEE.BuildingBlocks.Domain;
using System.Collections.Generic;
using System.Linq;
using IplanHEE.Admin.Domain.ApprovalTree.Events;

namespace IplanHEE.Admin.Domain.ApprovalTree
{
    public class Tree : Entity, IAggregateRoot
    {
        private readonly TreeId _id;
        private readonly string _name;
        private readonly string _label;
        private readonly List<SubTree> _subTrees;
        private Tree()
        {
            _subTrees = new List<SubTree>();
        }

        public void Publish(string userId)
        {
            this.CheckRule(new NodeLessSubTreeCannotBePublishedRule(_subTrees));
            this.CheckRule(new AnOrphanDataNodeCannotBePublishedRule(_subTrees));
            _subTrees.ForEach(st =>
            {
                st.PublishEditedNodes();
            });
            this.AddDomainEvent(new TreePublishedDomainEvent(this._id, userId));
        }

        public void Reset()
        {
            _subTrees.ForEach(st =>
            {
                st.ResetEditedNodes();
            });
        }

        /// <summary>
        /// To add a node to a specific subtree
        /// </summary>
        /// <param name="subTreeId"></param>
        /// <param name="nodeName"></param>
        /// 
        /// <param name="nodeType"></param>
        /// <param name="nodeLevel"></param>
        /// <param name="nodeIndex"></param>
        /// <param name="leafNode"></param>
        /// <param name="approvalLabel"></param>
        /// <param name="rejectLabel"></param>
        /// <param name="nodeMappingLevel"></param>
        /// <param name="nodeRoleModels"></param>
        /// <param name="validSubTreeCheckerForDataNode"></param>
        /// <param name="validDataNodeChecker"></param>
        public void AddNode(SubTreeId subTreeId, string nodeName, NodeTypeEnum nodeType,
            int nodeLevel, int nodeIndex, LeafNodeEnum leafNode, string approvalLabel, string rejectLabel, int nodeMappingLevel,
            List<NodeRoleModel> nodeRoleModels, IValidSubTreeCheckerForDataNode validSubTreeCheckerForDataNode,
            IValidDataNodeChecker validDataNodeChecker, IPushDownSubTreeMappedNodeValidator validator)
        {
            this.CheckRule(new IsValidSubTreeRule(_subTrees, subTreeId));
            var subTree = _subTrees.FirstOrDefault(x => x.Id == subTreeId);
            
            this.CheckRule(new PushUpSubTreeCanNotHaveMappedNodeRule(subTree?.IsPushDown ?? default, nodeMappingLevel));
            this.CheckRule(new AddingDataNodeShouldBeEnabledInTheSubtreeRule(subTree, nodeType));
            this.CheckRule(new AddingAuthorNodeNameCannotBeDuplicateInATreeRule(this.GetTreeAllNodes(), nodeName, nodeType));
            this.CheckRule(new ALowerOrderDataNodeLevelCannotSmallerThanHigherOrderDataNodeRule(this.GetSubTreeAllNodes(subTreeId), nodeType, nodeLevel, nodeIndex));
            this.CheckRule(new ALeafSubTreeDataNodeCannotAddIntoParentSubTreeRule(subTree, nodeType, nodeLevel, validSubTreeCheckerForDataNode));
            this.CheckRule(new IsDataNodeValidForRespectiveIndex(this._name, subTree?.Name, nodeType, nodeName, nodeIndex, nodeLevel, validDataNodeChecker));
            this.CheckRule(new PushDownSubTreeNodeMustHaveMappedNodeRule(subTree?.IsPushDown ?? false, nodeType, nodeIndex, nodeMappingLevel, AddNewNodeEnum.Yes, validator));
            // TODO : Create a adding node.
            var aNode = Node.AddNode(nodeName, nodeType, subTreeId: subTreeId,
                nodeLevel, nodeIndex, leafNode, approvalLabel, rejectLabel, nodeMappingLevel);

            // TODO : Get a list of NodeRole from NodeRoleModel list.
            var nodeRoles = GetNodeRolesFromModels(nodeRoleModels, aNode.Id);


            // TODO : Add roles to the node permission
            aNode.AddNodeRoles(nodeRoles);

            // TODO : Add node(Author/Data)
            subTree?.AddNodeAt(nodeIndex, aNode);

            this.UpdateNodesIndexFieldOfOperatingSubtree(subTree);

            subTree?.UpdateChildSubTreeSeedValue(_subTrees);

            this.UpdateTreesNodeLevel(_subTrees, operatingSubTree: subTree, operatingNodeIndex: nodeIndex);

            var nodes = subTree?.NodeList.Where(n => !n.IsPublish).OrderBy(n => n.NodeIndex).ToList();
            this.AddDomainEvent(new NodeAddedDomainEvent(aNode.Id, subTree?.Id, nodes?.IndexOf(aNode),
                nodes?[nodeIndex].NodeLevel, aNode.IsAuthorNode, aNode.IsLeafNode, aNode.Name,
                aNode.IsPublish, nodes?.Max(n => n.NodeLevel)));
        }

        public void RemoveNode(SubTreeId subTreeId, int nodeIndex)
        {
            this.CheckRule(new IsValidSubTreeRule(_subTrees, subTreeId));
            var subTree = _subTrees.FirstOrDefault(st => st.Id == subTreeId);
            var nodes = subTree
                ?.NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex).ToList();

            this.CheckRule(new IsIndexOutOfBoundForDeleteOperationRule<Node>(nodeIndex, nodes));
            var nodeId = nodes?[nodeIndex].Id;

            subTree?.RemoveNodeAt(nodeIndex);
            this.UpdateNodesIndexFieldOfOperatingSubtree(subTree);
            subTree?.UpdateChildSubTreeSeedValue(_subTrees);
            this.UpdateTreesNodeLevel(_subTrees, operatingSubTree: subTree, operatingNodeIndex: nodeIndex);

            nodes = subTree
                ?.NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex).ToList();
            this.AddDomainEvent(new NodeRemovedDomainEvent(subTree?.Id, nodeId, nodeIndex, nodes?.Count, nodes?.Max(n => n.NodeLevel)));
        }

        public void UpdateAuthorNode(SubTreeId subTreeId, NodeId nodeId, string nodeName,
            string approvalLabel, string rejectLabel, int nodeMappingLevel, List<NodeRoleModel> nodeRoleModels,
            IPushDownSubTreeMappedNodeValidator validator)
        {
            this.CheckRule(new IsValidSubTreeRule(_subTrees, subTreeId));
            var subTree = _subTrees
                ?.FirstOrDefault(st => st.Id == subTreeId);
            var nodeList = subTree
                ?.NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex).ToList();

            this.CheckRule(new IsValidAuthorNodeRule(nodeList, nodeId));
            this.CheckRule(new UpdatingAuthorNodeNameCannotBeDuplicateInATreeRule(this.GetTreeAllNodes(), nodeName, nodeId));
            this.CheckRule(new PushUpSubTreeCanNotHaveMappedNodeRule(subTree?.IsPushDown ?? default, nodeMappingLevel));
            var node = nodeList?.Find(n => n.Id == nodeId);
            this.CheckRule(new PushDownSubTreeNodeMustHaveMappedNodeRule(subTree?.IsPushDown ?? default
                , NodeTypeEnum.AuthorNode, node?.NodeIndex ?? default, nodeMappingLevel, AddNewNodeEnum.No, validator));

            node?.UpdateAuthorNode(nodeName, approvalLabel, rejectLabel, nodeMappingLevel, GetNodeRolesFromModels(nodeRoleModels, nodeId));

            AddDomainEvent(new NodeUpdatedDomainEvent(subTreeId, node?.Id, NodeTypeEnum.AuthorNode,
                node?.NodeIndex, node?.Name, node?.NodeLevel, node?.GetNodeRoles()));
        }


        public void UpdateDataNode(SubTreeId subTreeId, NodeId nodeId,
            string nodeName, int nodeLevel, IValidSubTreeCheckerForDataNode validSubTreeCheckerForDataNode,
            IValidDataNodeChecker validDataNodeChecker)
        {
            this.CheckRule(new IsValidSubTreeRule(_subTrees, subTreeId));
            var subTree = _subTrees
                ?.FirstOrDefault(st => st.Id == subTreeId);
            var nodes = subTree?.NodeList.Where(n => !n.IsPublish).OrderBy(n => n.NodeIndex).ToList();

            this.CheckRule(new IsValidDataNodeRule(nodes ?? new List<Node>(), nodeId));
            this.CheckRule(new ALeafSubTreeDataNodeCannotAddIntoParentSubTreeRule(subTree, NodeTypeEnum.DataNode, nodeLevel, validSubTreeCheckerForDataNode));

            var node = nodes?.Find(n => n.Id == nodeId);
            this.CheckRule(new IsDataNodeValidForRespectiveIndex(this._name, subTree?.Name, NodeTypeEnum.DataNode, nodeName,
                node?.NodeIndex ?? default, nodeLevel, validDataNodeChecker));

            node?.UpdateDataNode(nodeName, nodeLevel);

            AddDomainEvent(new NodeUpdatedDomainEvent(subTreeId, node?.Id, NodeTypeEnum.DataNode,
                node?.NodeIndex, node?.Name, node?.NodeLevel));
        }
        private List<Node> GetTreeAllNodes() =>
            _subTrees
                .SelectMany(subTree => subTree.NodeList)
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex)
                .ToList();

        private List<Node> GetParentSubTreeAllNodes(SubTreeId subTreeId)
        {
            var subTree = _subTrees.FirstOrDefault(st => st.Id == subTreeId);
            return _subTrees
                ?.FirstOrDefault(st => st.Id == subTree?.ParentSubTreeId)
                ?.NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex).ToList() ?? new List<Node>();
        }

        private List<Node> GetSubTreeAllNodes(SubTreeId subTreeId)
            => _subTrees?.FirstOrDefault(st => st.Id == subTreeId)
                ?.NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex)
                .ToList() ?? new List<Node>();
        private void UpdateNodesIndexFieldOfOperatingSubtree(SubTree subTree)
        {
            var operatingNodes = subTree?.NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex)
                .ToList();

            operatingNodes
                ?.ForEach(n => n.UpdateNodeIndexField(operatingNodes.IndexOf(n)));
        }

        private List<NodeRole> GetNodeRolesFromModels(List<NodeRoleModel> nodeRoleModels, NodeId nodeId)
        {
            return nodeRoleModels
                ?.Select(x => NodeRole.CreateNodeRole(x.RoleName, x.NodePermission, nodeId))
                .ToList() ?? new List<NodeRole>();
        }
        private void UpdateTreesNodeLevel(List<SubTree> subTrees, SubTree operatingSubTree, int operatingNodeIndex)
        {
            var nodes = operatingSubTree
                ?.NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex)
                .Skip(operatingNodeIndex)
                .Where(n => n.IsAuthorNode)
                .ToList();
            var pioneerNodeCount = operatingSubTree
                ?.NodeList
                .Where(n => !n.IsPublish)
                .OrderBy(n => n.NodeIndex)
                .Take(operatingNodeIndex)
                .Where(n => n.IsAuthorNode)
                ?.Count() ?? 0;

            nodes?.ForEach(n =>
            {
                n.UpdateAuthorNodeLevel(operatingSubTree?.SubTreeSeedValue, (nodes.IndexOf(n) + pioneerNodeCount));
            });

            var childSubtrees = subTrees
                .Where(st => st.ParentSubTreeId == operatingSubTree?.Id)
                .ToList();

            if (!childSubtrees.Any()) return;

            childSubtrees.ForEach(st =>
            {
                var authorNodes = st.NodeList
                    .Where(n => !n.IsPublish && n.IsAuthorNode)
                    .OrderBy(n => n.NodeIndex)
                    .ToList();
                authorNodes
                    .ForEach(n =>
                    {
                        n.UpdateAuthorNodeLevel(st.SubTreeSeedValue, authorNodes.IndexOf(n));
                    });
            });

        }


    }
}
