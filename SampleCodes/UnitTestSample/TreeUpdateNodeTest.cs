using IplanHEE.Admin.Domain.ApprovalTree;
using IplanHEE.Admin.Domain.ApprovalTree.Events;
using IplanHEE.Admin.Domain.ApprovalTree.Rules;
using IplanHEE.Admin.Domain.SharedLine;
using IplanHEE.Admin.Domain.UnitTests.SeedWork;
using IplanHEE.BuildingBlocks.Domain.PersistenceContract;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IplanHEE.Admin.Domain.UnitTests.ApprovalTree
{
    [TestFixture]
    public class TreeUpdateNodeTest : TreeTestBase
    {
        private readonly TreeId _budgetApprovalTreeId;
        private SubTreeId _tafeCommissionSubTreeId;
        private SubTreeId _tafeCorporateSubTreeId;
        private TreeTestData _treeTestData;
        public TreeUpdateNodeTest()
        {
            _budgetApprovalTreeId = new TreeId(Guid.NewGuid());
            _tafeCommissionSubTreeId = new SubTreeId(Guid.NewGuid());
            _tafeCorporateSubTreeId = new SubTreeId(Guid.NewGuid());
            _treeTestData = AddSubTreeTestData(CreateTreeTestData(new TreeTestDataOptions
            {
                TreeId = _budgetApprovalTreeId,
                TreeName = DomainKeyValueProperties.BudgetApprovalTree
            }));
        }

        private TreeTestData AddSubTreeTestData(TreeTestData treeTestData)
        {
            treeTestData = AddSubTreeWithTreeTestData(new TreeTestDataOptions
            {
                SubTreeId = _tafeCommissionSubTreeId,
                SubTreeName = DomainKeyValueProperties.TafeCommissionSubTree,
                IsDataNodeEnable = true,
                IsLeafSubTree = false,
                TreeId = _budgetApprovalTreeId
            }, treeTestData);
            treeTestData = AddSubTreeWithTreeTestData(new TreeTestDataOptions
            {
                SubTreeId = _tafeCorporateSubTreeId,
                SubTreeName = "TAFE Corporate",
                ParentSubTreeId = _tafeCommissionSubTreeId,
                IsDataNodeEnable = true,
                IsLeafSubTree = true,
                TreeId = _budgetApprovalTreeId
            }, treeTestData);
            return treeTestData;
        }
        [Test]
        public void UpdateAuthorNode_IsSuccessful()
        {
            var treeAddNodeTest = new TreeUpdateNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(1).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Region", 0, 1).Returns(x => true);

            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0, true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Region", NodeTypeEnum.DataNode, 1, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "RGM", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "RGM Approved", "RGM Rejected",
                default, new List<NodeRoleModel>
                {
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "Senior RGM"},
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ViewOnly, RoleName = "Senior RGM ViewOnly"}
            }, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            var nodeAddedDomainEvent = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .SingleOrDefault(e => e.IsAuthorNode && e.NodeName == "RGM");

            _treeTestData.Tree.UpdateAuthorNode(_tafeCommissionSubTreeId, nodeAddedDomainEvent?.NodeId,
                "UpdatedRGM", "U_RGM Approved", "U_RGM Rejected",
                default, new List<NodeRoleModel>
                {
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "U_Senior RGM"},
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ViewOnly, RoleName = "U_Senior RGM ViewOnly"}
                }, pushDownSubTreeMappedNodeValidator);
            var nodeUpdatedDomainEvent = AssertPublishedDomainEvent<NodeUpdatedDomainEvent>(_treeTestData.Tree);

            Assert.That(nodeUpdatedDomainEvent.SubTreeId, Is.EqualTo(_tafeCommissionSubTreeId));
            Assert.That(nodeUpdatedDomainEvent.NodeId, Is.EqualTo(nodeAddedDomainEvent?.NodeId));
            Assert.That(nodeUpdatedDomainEvent.NodeIndex, Is.EqualTo(nodeAddedDomainEvent?.NodeIndex));
            Assert.That(nodeUpdatedDomainEvent.NodeLevel, Is.EqualTo(nodeAddedDomainEvent?.NodeLevel));
            Assert.That(nodeUpdatedDomainEvent.NodeName, Is.EqualTo("UpdatedRGM"));
            Assert.That(nodeUpdatedDomainEvent.NodeRoles.Count, Is.EqualTo(2));
            Assert.IsTrue(nodeUpdatedDomainEvent.NodeRoles
                .Any(n => n.NodePermission == NodePermissionTypeEnum.ApproveReject
                        && n.RoleName == "U_Senior RGM"));

            Assert.IsTrue(nodeUpdatedDomainEvent.NodeRoles
                .Any(n => n.NodePermission == NodePermissionTypeEnum.ViewOnly
                          && n.RoleName == "U_Senior RGM ViewOnly"));
        }

        [Test]
        public void UpdateDataNode_IsSuccessful()
        {
            var treeAddNodeTest = new TreeUpdateNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(1).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Region", 0, 1).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0, true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Region", NodeTypeEnum.DataNode, 1, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "RGM", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "RGM Approved", "RGM Rejected",
                default, new List<NodeRoleModel>
                {
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "Senior RGM"},
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ViewOnly, RoleName = "Senior RGM ViewOnly"}
            }, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            var nodeAddedDomainEvent = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .SingleOrDefault(e => !e.IsAuthorNode && e.NodeName == "Region");

            validSubTreeCheckerForDataNode.IsValidSubTree(2).Returns(x => true);
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Institute", 1, 2).Returns(x => true);

            _treeTestData.Tree.UpdateDataNode(_tafeCommissionSubTreeId, nodeAddedDomainEvent?.NodeId,
                "Institute", 2, validSubTreeCheckerForDataNode, validDataNodeChecker);

            var nodeUpdatedDomainEvent = AssertPublishedDomainEvent<NodeUpdatedDomainEvent>(_treeTestData.Tree);

            Assert.That(nodeUpdatedDomainEvent.SubTreeId, Is.EqualTo(_tafeCommissionSubTreeId));
            Assert.That(nodeUpdatedDomainEvent.NodeId, Is.EqualTo(nodeAddedDomainEvent?.NodeId));
            Assert.That(nodeUpdatedDomainEvent.NodeLevel, Is.EqualTo(2));
            Assert.That(nodeUpdatedDomainEvent.NodeName, Is.EqualTo("Institute"));

        }

        [Test]
        public void UpdateDataNode_WhenNodeIdInvalid_BreakIsValidDataNodeRule()
        {
            var treeAddNodeTest = new TreeUpdateNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(1).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Region", 0, 1).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0, true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Region", NodeTypeEnum.DataNode, 1, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "RGM", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "RGM Approved", "RGM Rejected",
                default, new List<NodeRoleModel>
                {
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "Senior RGM"},
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ViewOnly, RoleName = "Senior RGM ViewOnly"}
            }, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            validSubTreeCheckerForDataNode.IsValidSubTree(2).Returns(x => true);

            var nodeAddedDomainEvent = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .SingleOrDefault(e => e.IsAuthorNode && e.NodeName == "RGM");

            AssertBrokenRule<IsValidDataNodeRule>(() =>
            {
                _treeTestData.Tree.UpdateDataNode(_tafeCommissionSubTreeId,
                    nodeAddedDomainEvent?.NodeId, "Institute", 2, validSubTreeCheckerForDataNode, validDataNodeChecker);
            });

        }

        [Test]
        public void UpdateAuthorNode_WhenNodeIdInvalid_BreakIsValidAuthorNodeRule()
        {
            var treeAddNodeTest = new TreeUpdateNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(1).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Region", 0, 1).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0, true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Region", NodeTypeEnum.DataNode, 1, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "RGM", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "RGM Approved", "RGM Rejected",
                default, new List<NodeRoleModel>
                {
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "Senior RGM"},
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ViewOnly, RoleName = "Senior RGM ViewOnly"}
                }, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);


            var nodeAddedDomainEvent = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .SingleOrDefault(e => !e.IsAuthorNode && e.NodeName == "Region");

            AssertBrokenRule<IsValidAuthorNodeRule>(() =>
            {
                _treeTestData.Tree.UpdateAuthorNode(_tafeCommissionSubTreeId,
                    nodeAddedDomainEvent?.NodeId, "UpdateRGM", string.Empty, string.Empty, default, null, pushDownSubTreeMappedNodeValidator);
            });

        }
        [Test]
        public void UpdateDataNode_WhenInsertingSubTreeNodeIntoParentSubTree_BreakALeafSubTreeDataNodeCannotAddIntoParentSubTreeRule()
        {
            var treeAddNodeTest = new TreeUpdateNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(1).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Region", 0, 1).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0, true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Region", NodeTypeEnum.DataNode, 1, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "RGM", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "RGM Approved", "RGM Rejected",
                default, new List<NodeRoleModel>
                {
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "Senior RGM"},
                    new NodeRoleModel{NodePermission = NodePermissionTypeEnum.ViewOnly, RoleName = "Senior RGM ViewOnly"}
                }, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);


            var nodeAddedDomainEvent = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .SingleOrDefault(e => !e.IsAuthorNode && e.NodeName == "Region");

            AssertBrokenRule<ALeafSubTreeDataNodeCannotAddIntoParentSubTreeRule>(() =>
            {
                validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                    DomainKeyValueProperties.TafeCommissionSubTree, "Skill Team", 1, 3).Returns(x => true);

                _treeTestData.Tree.UpdateDataNode(_tafeCommissionSubTreeId,
                    nodeAddedDomainEvent?.NodeId, "Skill Team", 3, validSubTreeCheckerForDataNode, validDataNodeChecker);
            });

        }
    }
}
