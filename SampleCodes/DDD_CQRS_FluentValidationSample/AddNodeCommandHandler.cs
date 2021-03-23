using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IplanHEE.Admin.Application.Configuration.Commands;
using IplanHEE.Admin.Domain.ApprovalTree;
using MediatR;

namespace IplanHEE.Admin.Application.ApprovalTree.AddNode
{
    public class AddNodeCommandHandler : ICommandHandler<AddNodeCommand>
    {
        private readonly ITreeRepository _treeRepository;
        private readonly IValidSubTreeCheckerForDataNode _validSubTreeCheckerForDataNode;
        private readonly IValidDataNodeChecker _validDataNodeChecker;
        private readonly IPushDownSubTreeMappedNodeValidator _validator;

        public AddNodeCommandHandler(ITreeRepository treeRepository,
            IValidSubTreeCheckerForDataNode validSubTreeCheckerForDataNode,
            IValidDataNodeChecker validDataNodeChecker,
            IPushDownSubTreeMappedNodeValidator validator)
        {
            _treeRepository = treeRepository;
            _validSubTreeCheckerForDataNode = validSubTreeCheckerForDataNode;
            _validDataNodeChecker = validDataNodeChecker;
            _validator = validator;
        }
        public async Task<Unit> Handle(AddNodeCommand command, CancellationToken cancellationToken)
        {
            var tree = await _treeRepository.GetTreeById(command.TreeId);
            tree.AddNode(command.SubTreeId, command.NodeName, command.NodeType, command.NodeLevel,
                command.NodeIndex, command.LeafNode, command.ApprovalLabel, command.RejectLabel,
                command.NodeMappingLevel, command.NodeRoleModels, _validSubTreeCheckerForDataNode,
                _validDataNodeChecker, _validator);

            return Unit.Value;
        }
    }
}
