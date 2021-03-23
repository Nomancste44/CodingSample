using IplanHEE.Admin.Domain.ApprovalTree;
using IplanHEE.BuildingBlocks.Domain.PersistenceContract;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using IplanHEE.Admin.Domain.ApprovalTree.Events;
using IplanHEE.Admin.Domain.ApprovalTree.Rules;
using IplanHEE.Admin.Domain.SharedLine;
using IplanHEE.Admin.Domain.UnitTests.SeedWork;
using NSubstitute;
namespace IplanHEE.Admin.Domain.UnitTests.ApprovalTree
{
    [TestFixture]
    public class TreeAddNodeTest : TreeTestBase
    {
        private readonly TreeId _budgetApprovalTreeId;
        private SubTreeId _tafeCommissionSubTreeId;
        private SubTreeId _tafeCorporateSubTreeId;
        private SubTreeId _productBuilderSubTreeId;
        private TreeTestData _treeTestData;
        public TreeAddNodeTest()
        {
            _budgetApprovalTreeId = new TreeId(Guid.NewGuid());
            _tafeCorporateSubTreeId = new SubTreeId(Guid.NewGuid());
            _tafeCommissionSubTreeId = new SubTreeId(Guid.NewGuid());
            _productBuilderSubTreeId = new SubTreeId(Guid.NewGuid());
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

            treeTestData = AddSubTreeWithTreeTestData(new TreeTestDataOptions
            {
                SubTreeId = _productBuilderSubTreeId,
                SubTreeName = "Product Builder",
                IsDataNodeEnable = false,
                IsLeafSubTree = true,
                TreeId = _budgetApprovalTreeId
            }, treeTestData);
            return treeTestData;
        }
        [Test]
        public void AddNode_WhenNodeTypeIsDataNode_IsSuccessful()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            var nodeAddedDomainEvent = AssertPublishedDomainEvent<NodeAddedDomainEvent>(_treeTestData.Tree);
            Assert.That(nodeAddedDomainEvent.SubTreeId, Is.EqualTo(_tafeCorporateSubTreeId));
            Assert.That(nodeAddedDomainEvent.NodeIndex, Is.EqualTo(0));
            Assert.That(nodeAddedDomainEvent.NodeLevel, Is.EqualTo(4));
            Assert.That(nodeAddedDomainEvent.IsPublish, Is.EqualTo(false));
            Assert.That(nodeAddedDomainEvent.IsLeafNode, Is.EqualTo(false));
            Assert.That(nodeAddedDomainEvent.NodeName, Is.EqualTo("Skill Team"));
        }

        [Test]
        public void AddNode_WhenAddingMultipleAuthorNodeInASubTreeToCalculateNodeLevel_IsSuccessful()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode2", NodeTypeEnum.AuthorNode, 4, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode3", NodeTypeEnum.AuthorNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode4", NodeTypeEnum.AuthorNode, 4, 2, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode5", NodeTypeEnum.AuthorNode, 4, 3, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            var nodeAddedDomainEvents = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .Where(e => e.IsAuthorNode)
                .OrderByDescending(e => e.MaxNodeLevel)
                .ToList();

            Assert.That(nodeAddedDomainEvents.Count, Is.EqualTo(5));
            Assert.That(nodeAddedDomainEvents[0].MaxNodeLevel, Is.EqualTo(5));
        }
        [Test]
        public void AddNode_WhenAddingMultipleAuthorNodeInAParentTreeToCalculateNodeLevel_IsSuccessful()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(2).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team Unit", 0, 2).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 2, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            validSubTreeCheckerForDataNode.IsValidSubTree(1).Returns(x => true);
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Region", 0, 1).Returns(x => true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Region", NodeTypeEnum.DataNode, 1, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);


            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode2", NodeTypeEnum.AuthorNode, 0, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode2", NodeTypeEnum.AuthorNode, 0, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode3", NodeTypeEnum.AuthorNode, 0, 2, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            var nodeAddedDomainEvents = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .Where(e => e.IsAuthorNode)
                .OrderByDescending(e => e.MaxNodeLevel)
                .ToList();

            Assert.That(nodeAddedDomainEvents.Count, Is.EqualTo(5));
            Assert.That(nodeAddedDomainEvents[0].MaxNodeLevel, Is.EqualTo(3));
        }

        [Test]
        public void AddNode_WhenAddingMultipleAuthorNodeInAParentAndASubTreeToCalculateNodeLevel_IsSuccessful()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 3).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 3, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            validSubTreeCheckerForDataNode.IsValidSubTree(2).Returns(x => true);
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Institute", 0, 2).Returns(x => true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Institute", NodeTypeEnum.DataNode, 2, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);


            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode2", NodeTypeEnum.AuthorNode, 0, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode2", NodeTypeEnum.AuthorNode, 0, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode3", NodeTypeEnum.AuthorNode, 0, 2, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            var nodeAddedDomainEvents = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .Where(e => e.IsAuthorNode)
                .OrderByDescending(e => e.MaxNodeLevel)
                .ToList();

            Assert.That(nodeAddedDomainEvents.Count, Is.EqualTo(5));
            Assert.That(nodeAddedDomainEvents[0].MaxNodeLevel, Is.EqualTo(5));
        }

        [Test]
        public void AddNode_WhenDataNodeIsGoingToAddConsecutively_BreakADataNodeCannotFollowAnotherDataNodeConsecutivelyRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(3).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team Unit", 0, 3).Returns(x => true);

            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 3, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            AssertBrokenRule<ADataNodeCannotFollowAnotherDataNodeConsecutivelyRule>(() =>
            {
                validSubTreeCheckerForDataNode.IsValidSubTree(2).Returns(x => true);
                validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                    "TAFE Corporate", "Skill Team Unit", 0, 2).Returns(x => true);

                _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 2, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });

        }

        [Test]
        public void
            AddNode_WhenDataNodeNameIsAlreadyExistInASubTree_BreakADataNodeNameCannotBeDuplicatedInASubTreeRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(3).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team Unit", 0, 3).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);


            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 3, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Host AuthorNode", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "Host Approved", "Host Reject",
                default, new List<NodeRoleModel> { new NodeRoleModel { NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "HOST" } },
                validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team Unit", 0, 2).Returns(x => true);

            AssertBrokenRule<ADataNodeNameCannotBeDuplicatedInASubTreeRule>(() =>
            {
                _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 2, 0, LeafNodeEnum.No,
                    string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });
        }

        [Test]
        public void
            AddNode_WhenDataNodeAddingNotEnabledInSubTree_BreakAddingDataNodeShouldBeEnabledInTheSubtreeRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._productBuilderSubTreeId = treeAddNodeTest._productBuilderSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(3).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "Product Builder", "ProductDataNode", 0, 3).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            AssertBrokenRule<AddingDataNodeShouldBeEnabledInTheSubtreeRule>(() =>
            {
                _treeTestData.Tree.AddNode(_productBuilderSubTreeId, "ProductDataNode", NodeTypeEnum.DataNode, 3, 0, LeafNodeEnum.Yes,
                    string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });
        }

        [Test]
        public void AddNode_WhenALeafSubTreeDataNodeIsGoingToIntoParentSubTree_BreakALeafSubTreeDataNodeCannotAddIntoParentSubTreeRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(3).Returns(x => false);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team Unit", 0, 3).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            AssertBrokenRule<ALeafSubTreeDataNodeCannotAddIntoParentSubTreeRule>(() =>
            {
                _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 3, 0, LeafNodeEnum.Yes,
                    string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });
        }

        [Test]
        public void AddNode_WhenADataNodeLevelIsLowerThanAboveOne_BreakALowerOrderDataNodeLevelCannotSmallerThanHigherOrderDataNodeRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);


            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Host AuthorNode", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "Host Approved", "Host Reject",
                default, new List<NodeRoleModel> { new NodeRoleModel { NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "HOST" } },
                validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);


            AssertBrokenRule<ALowerOrderDataNodeLevelCannotSmallerThanHigherOrderDataNodeRule>(() =>
            {
                validSubTreeCheckerForDataNode.IsValidSubTree(3).Returns(x => true);
                validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                    "TAFE Corporate", "Skill Team Unit", 2, 3).Returns(x => true);

                _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 3, 2, LeafNodeEnum.No,
                    string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });
        }

        [Test]
        public void AddNode_WhenADataNodeLevelIsGreaterThanBelowOne_BreakALowerOrderDataNodeLevelCannotSmallerThanHigherOrderDataNodeRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);

            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Host AuthorNode", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "Host Approved", "Host Reject",
                default, new List<NodeRoleModel> { new NodeRoleModel { NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "HOST" } },
                validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);


            AssertBrokenRule<ALowerOrderDataNodeLevelCannotSmallerThanHigherOrderDataNodeRule>(() =>
            {

                validSubTreeCheckerForDataNode.IsValidSubTree(5).Returns(x => true);
                validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                    "TAFE Corporate", "Skill Team Unit", 0, 5).Returns(x => true);

                _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 5, 0, LeafNodeEnum.No,
                    string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });
        }

        [Test]
        public void AddNode_WhenAddingAnAuthorNodeWithoutHavingAnyDataNode_BreakAnAuthorNodeMustBeFollowedByADataNodeRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            AssertBrokenRule<AnAuthorNodeMustBeFollowedByADataNodeRule>(() =>
            {
                _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Host AuthorNode", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                    "Host Approved", "Host Reject",
                    default, new List<NodeRoleModel> { new NodeRoleModel { NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "HOST" } },
                    validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });
        }

        [Test]
        public void AddNode_WhenAddingAnAuthorNodeHavingDuplicateName_BreakAuthorNodeNameCannotBeDuplicateInATreeRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            this._tafeCommissionSubTreeId = treeAddNodeTest._tafeCommissionSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Host AuthorNode", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                "Host Approved", "Host Reject",
                default, new List<NodeRoleModel> { new NodeRoleModel { NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "HOST" } },
                validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            validSubTreeCheckerForDataNode.IsValidSubTree(2).Returns(x => true);
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Institute", 0, 2).Returns(x => true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Institute", NodeTypeEnum.DataNode, 2, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            AssertBrokenRule<AddingAuthorNodeNameCannotBeDuplicateInATreeRule>(() =>
            {
                _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Host AuthorNode", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No, string.Empty, string.Empty,
                    default, new List<NodeRoleModel> { new NodeRoleModel { NodePermission = NodePermissionTypeEnum.ApproveReject, RoleName = "HOST" } }, validSubTreeCheckerForDataNode,
                    validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });

        }

        [Test]
        public void AddNode_WhenNodeIndexIsNegative_BreakIsIndexOutOfBoundForAddOperationRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", -1, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            AssertBrokenRule<IsIndexOutOfBoundForAddOperationRule<Node>>(() =>
            {
                _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, -1, LeafNodeEnum.Yes,
                    string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });

        }

        [Test]
        public void AddNode_WhenNodeIndexIsBiggerThanLength_BreakIsIndexOutOfBoundForAddOperationRule()
        {
            var treeAddNodeTest = new TreeAddNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;
            DomainEventsTestHelper.ClearAllDomainEvents(_treeTestData.Tree);

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 1, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            AssertBrokenRule<IsIndexOutOfBoundForAddOperationRule<Node>>(() =>
            {
                _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 1, LeafNodeEnum.Yes,
                    string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            });

        }

    }
}
