using System;
using System.Linq;
using IplanHEE.Admin.Domain.ApprovalTree;
using IplanHEE.Admin.Domain.ApprovalTree.Events;
using IplanHEE.Admin.Domain.ApprovalTree.Rules;
using IplanHEE.Admin.Domain.SharedLine;
using IplanHEE.Admin.Domain.UnitTests.SeedWork;
using IplanHEE.BuildingBlocks.Domain.PersistenceContract;
using NSubstitute;
using NUnit.Framework;

namespace IplanHEE.Admin.Domain.UnitTests.ApprovalTree
{
    [TestFixture]
    public class TreeRemoveNodeTest : TreeTestBase
    {
        private readonly TreeId _budgetApprovalTreeId;
        private SubTreeId _tafeCommissionSubTreeId;
        private SubTreeId _tafeCorporateSubTreeId;
        private SubTreeId _productBuilderSubTreeId;
        private TreeTestData _treeTestData;
        public TreeRemoveNodeTest()
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
        public void RemoveNode_WhenNodeTypeIsAuthorNode_IsSuccessful()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode2", NodeTypeEnum.AuthorNode, 4, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 1);

            var removingNodeDomainEvent = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .SingleOrDefault(e => e.NodeName == "AuthorNode2");

            var nodeRemovedDomainEvent = AssertPublishedDomainEvent<NodeRemovedDomainEvent>(_treeTestData.Tree);

            Assert.That(nodeRemovedDomainEvent.NodeId, Is.EqualTo(removingNodeDomainEvent?.NodeId));
            Assert.That(nodeRemovedDomainEvent.SubTreeId, Is.EqualTo(_tafeCorporateSubTreeId));
            Assert.That(nodeRemovedDomainEvent.CurrentNodeCount, Is.EqualTo(2));
            Assert.That(nodeRemovedDomainEvent.NodeIndex, Is.EqualTo(1));
        }
        [Test]
        public void RemoveNode_WhenNodeTypeIsDataNode_IsSuccessful()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            validSubTreeCheckerForDataNode.IsValidSubTree(3).Returns(x => true);
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team Unit", 0, 3).Returns(x => true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 3, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 0);

            var removingNodeDomainEvent = AssertPublishedDomainEvents<NodeAddedDomainEvent>(_treeTestData.Tree)
                .SingleOrDefault(e => e.NodeName == "Skill Team Unit");

            var nodeRemovedDomainEvent = AssertPublishedDomainEvent<NodeRemovedDomainEvent>(_treeTestData.Tree);

            Assert.That(nodeRemovedDomainEvent.NodeId, Is.EqualTo(removingNodeDomainEvent?.NodeId));
            Assert.That(nodeRemovedDomainEvent.SubTreeId, Is.EqualTo(_tafeCorporateSubTreeId));
            Assert.That(nodeRemovedDomainEvent.CurrentNodeCount, Is.EqualTo(2));
            Assert.That(nodeRemovedDomainEvent.NodeIndex, Is.EqualTo(0));
        }

        [Test]
        public void RemoveNode_WhenDeletingAnAuthorNodeBetweenTwoDataNodes_BreakDeletingAnAuthorNodeCannotLeftTwoDataNodeConsecutivelyRule()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            validSubTreeCheckerForDataNode.IsValidSubTree(3).Returns(x => true);
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team Unit", 0, 3).Returns(x => true);


            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team Unit", NodeTypeEnum.DataNode, 3, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            AssertBrokenRule<DeletingAnAuthorNodeCannotLeftTwoDataNodeConsecutivelyRule>(() =>
            {
                _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 1);
            });
        }

        [Test]
        public void RemoveNode_WhenThereIsOnlyAnDataNodeExist_BreakDeletingADataNodeCannotOrphanAuthorNodesRule()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode2", NodeTypeEnum.AuthorNode, 4, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            AssertBrokenRule<DeletingADataNodeCannotOrphanAuthorNodesRule>(() =>
            {
                _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 2);
            });

        }

        [Test]
        public void RemoveNode_WhenNodeIndexIsEqualToNodes_BreakIsIndexOutOfBoundForDeleteOperationRule()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            AssertBrokenRule<IsIndexOutOfBoundForDeleteOperationRule<Node>>(() =>
            {
                _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 1);
            });

        }
        [Test]
        public void RemoveNode_WhenRemovingNodeIsLeafNode_BreakCannotDeleteLeafDataNodeRule()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            AssertBrokenRule<CannotDeleteLeafDataNodeRule>(() =>
            {
                _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 0);
            });

        }

        [Test]
        public void RemoveNode_WhenSubTreeIdIsNotValid_BreakIsValidSubTreeRule()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
            this._tafeCorporateSubTreeId = treeAddNodeTest._tafeCorporateSubTreeId;

            var validSubTreeCheckerForDataNode = Substitute.For<IValidSubTreeCheckerForDataNode>();
            validSubTreeCheckerForDataNode.IsValidSubTree(4).Returns(x => true);
            var validDataNodeChecker = Substitute.For<IValidDataNodeChecker>();
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                "TAFE Corporate", "Skill Team", 0, 4).Returns(x => true);
            var pushDownSubTreeMappedNodeValidator = Substitute.For<IPushDownSubTreeMappedNodeValidator>();
            pushDownSubTreeMappedNodeValidator.IsValidMappedNode(0, 0,true).ReturnsForAnyArgs(true);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "Skill Team", NodeTypeEnum.DataNode, 4, 0, LeafNodeEnum.Yes,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker, pushDownSubTreeMappedNodeValidator);

            AssertBrokenRule<IsValidSubTreeRule>(() =>
            {
                _treeTestData.Tree.RemoveNode(_tafeCommissionSubTreeId, 0);
            });

        }

        [Test]
        public void Remove_WhenRemovingMultipleAuthorNodeInASubTreeToCalculateNodeLevel_IsSuccessful()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
            this._treeTestData = treeAddNodeTest._treeTestData;
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
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode2", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode3", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode4", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode5", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 2);
            _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 2);

            var nodeRemovedDomainEvents = AssertPublishedDomainEvents<NodeRemovedDomainEvent>(_treeTestData.Tree)
                .OrderByDescending(e => e.OccurredOn)
                .ToList();

            Assert.That(nodeRemovedDomainEvents[0].MaxNodeLevel, Is.EqualTo(3));
            Assert.That(nodeRemovedDomainEvents[1].MaxNodeLevel, Is.EqualTo(4));

        }
        [Test]
        public void RemoveNode_WhenRemovingMultipleAuthorNodeInAParentTreeToCalculateNodeLevel_IsSuccessful()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
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
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode2", NodeTypeEnum.AuthorNode, 0, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode2", NodeTypeEnum.AuthorNode, 0, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode3", NodeTypeEnum.AuthorNode, 0, 2, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.RemoveNode(_tafeCommissionSubTreeId, 0);
            _treeTestData.Tree.RemoveNode(_tafeCommissionSubTreeId, 0);

            var nodeRemovedDomainEvents = AssertPublishedDomainEvents<NodeRemovedDomainEvent>(_treeTestData.Tree)
                .OrderByDescending(e => e.OccurredOn)
                .ToList();

            Assert.That(nodeRemovedDomainEvents[0].MaxNodeLevel, Is.EqualTo(1));
            Assert.That(nodeRemovedDomainEvents[1].MaxNodeLevel, Is.EqualTo(2));
        }

        [Test]
        public void RemoveNode_WhenRemovingMultipleAuthorNodeInAParentAndASubTreeToCalculateNodeLevel_IsSuccessful()
        {
            var treeAddNodeTest = new TreeRemoveNodeTest();
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
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);

            validSubTreeCheckerForDataNode.IsValidSubTree(1).Returns(x => true);
            validDataNodeChecker.IsValidDataNode(DomainKeyValueProperties.BudgetApprovalTree,
                DomainKeyValueProperties.TafeCommissionSubTree, "Region", 0, 1).Returns(x => true);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "Region", NodeTypeEnum.DataNode, 1, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);


            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode2", NodeTypeEnum.AuthorNode, 0, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCorporateSubTreeId, "AuthorNode3", NodeTypeEnum.AuthorNode, 0, 2, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode1", NodeTypeEnum.AuthorNode, 0, 0, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);
            _treeTestData.Tree.AddNode(_tafeCommissionSubTreeId, "ParentAuthorNode2", NodeTypeEnum.AuthorNode, 0, 1, LeafNodeEnum.No,
                string.Empty, string.Empty, default, null, validSubTreeCheckerForDataNode, validDataNodeChecker,pushDownSubTreeMappedNodeValidator);

            _treeTestData.Tree.RemoveNode(_tafeCommissionSubTreeId, 0);
            _treeTestData.Tree.RemoveNode(_tafeCorporateSubTreeId, 0);


            var nodeRemovedDomainEvents = AssertPublishedDomainEvents<NodeRemovedDomainEvent>(_treeTestData.Tree)
                .OrderByDescending(e => e.OccurredOn)
                .ToList();

            Assert.That(nodeRemovedDomainEvents[0].MaxNodeLevel, Is.EqualTo(3));
            Assert.That(nodeRemovedDomainEvents[1].MaxNodeLevel, Is.EqualTo(1));
        }
    }
}
