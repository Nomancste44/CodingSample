using System;
using System.Collections.Generic;
using System.Text;
using IplanHEE.Admin.Application.Contracts;
using IplanHEE.Admin.Domain.ApprovalTree;
using IplanHEE.Admin.Domain.SharedLine;

namespace IplanHEE.Admin.Application.ApprovalTree.AddNode
{
    public class AddNodeCommand : CommandBase
    {
        public string RawTreeId { get; }
        public string RawSubTreeId { get; }
        public TreeId TreeId { get; }
        public SubTreeId SubTreeId { get; }
        public string NodeName { get; }
        public NodeTypeEnum NodeType { get; }
        public int NodeLevel { get; }
        public int NodeIndex { get; }
        public LeafNodeEnum LeafNode { get; }
        public string ApprovalLabel { get; }
        public string RejectLabel { get; }
        public int NodeMappingLevel { get; }

        public List<NodeRoleModel> NodeRoleModels { get; }

        public AddNodeCommand(Guid treeId, Guid subTreeId, string nodeName, NodeTypeEnum nodeType,
            int nodeLevel, int nodeIndex, LeafNodeEnum leafNode, string approvalLabel, string rejectLabel,
            int nodeMappingLevel, List<NodeRoleModel> nodeRoleModels)
        {
            RawTreeId = treeId.ToString();
            RawSubTreeId = subTreeId.ToString();
            TreeId = new TreeId(Guid.Parse(RawTreeId));
            SubTreeId = new SubTreeId(Guid.Parse(RawSubTreeId));
            NodeName = nodeName;
            NodeType = nodeType;
            NodeLevel = nodeLevel;
            NodeIndex = nodeIndex;
            LeafNode = leafNode;
            ApprovalLabel = approvalLabel;
            RejectLabel = rejectLabel;
            NodeMappingLevel = nodeMappingLevel;
            NodeRoleModels = nodeRoleModels;
        }
    }
}
