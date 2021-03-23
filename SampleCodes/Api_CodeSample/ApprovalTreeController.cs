using IplanHEE.Admin.Application.ApprovalTree.AddNode;
using IplanHEE.Admin.Application.ApprovalTree.GetApprovalTrees;
using IplanHEE.Admin.Application.ApprovalTree.GetAuthorNodes;
using IplanHEE.Admin.Application.ApprovalTree.GetTreeNodes;
using IplanHEE.Admin.Application.ApprovalTree.GetTrees;
using IplanHEE.Admin.Application.ApprovalTree.PublishTree;
using IplanHEE.Admin.Application.ApprovalTree.RemoveNode;
using IplanHEE.Admin.Application.ApprovalTree.ResetTree;
using IplanHEE.Admin.Application.ApprovalTree.UpdateNode;
using IplanHEE.Admin.Application.Configuration.Authentication;
using IplanHEE.Admin.Application.Contracts;
using IplanHEE.Admin.Domain.SharedLine;
using IplanHEE.Api.Authentication.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IplanHEE.Admin.Application.ApprovalTree.GetMappingNodes;
using IplanHEE.BuildingBlocks.Application;

namespace IplanHEE.Api.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [HasPermission(PolicyClaimTypes.Admin)]
    public class ApprovalTreeController : ControllerBase
    {
        private readonly IAdminModule _adminModule;
        private readonly IExecutionContextAccessor _executionContextAccessor;

        public ApprovalTreeController(IAdminModule adminModule,
            IExecutionContextAccessor executionContextAccessor)
        {
            _adminModule = adminModule;
            _executionContextAccessor = executionContextAccessor;
        }

        /// <summary>
        /// To get all available trees
        /// </summary>
        /// <returns></returns>
        [HttpGet(nameof(GetAllTrees))]
        public async Task<IEnumerable<TreeDto>> GetAllTrees()
        {
            return await _adminModule.ExecuteQueryAsync(new GetAllTreesQuery());
        }

        /// <summary>
        /// To get all nodes of a tree to show in publish or edit mode.
        /// </summary>
        /// <param name="treeName">Name of three</param>
        /// <param name="treeState">
        /// State of the respective tree whether it's publish or edit.
        /// 0 denotes edit and 1 for publish</param>
        /// <returns></returns>
        [HttpGet(nameof(GetTreeNodes))]
        public async Task<IEnumerable<TreeNodesDto>> GetTreeNodes(string treeName, TreeState treeState)
        {
            return await _adminModule.ExecuteQueryAsync(new GetTreeNodesQuery(treeName, treeState));
        }

        /// <summary>
        /// To Update an Author Node, get the Author node.
        /// </summary>
        /// <param name="nodeId">Node Id(Guid) of the respective Author node.</param>
        /// <param name="treeState">
        /// State of the respective tree whether it's publish or edit.
        /// 0 denotes edit and 1 for publish</param>
        /// <returns></returns>
        [HttpGet(nameof(GetAuthorNodeById))]
        public async Task<AuthorNodeDto> GetAuthorNodeById(Guid nodeId, TreeState treeState)
        {
            return await _adminModule.ExecuteQueryAsync(new GetAuthorNodeByIdQuery(nodeId, treeState));
        }

        ///  <summary>
        ///  To add or update a data node.
        ///  Get all available data nodes for the respective node level
        ///  </summary>
        ///  <param name="treeName">Name of the respective tree</param>
        ///  <param name="subTreeName">Name of the respective sub tree</param>
        ///  <param name="nodeIndex">Adding or Updating node position index</param>
        ///  <param name="nodeLevel">To update the data node, set current node level.
        /// To add a data node. zero is default.
        ///  </param>
        ///  <returns></returns>
        [HttpGet(nameof(GetDataNodes))]
        public async Task<IEnumerable<DataNodeDto>> GetDataNodes(string treeName, string subTreeName, int nodeLevel, int nodeIndex)
        {
            return await _adminModule.ExecuteQueryAsync(new GetDataNodesQuery(treeName, subTreeName, nodeLevel, nodeIndex));
        }

        /// <summary>
        /// To add an Author node, get all available roles
        /// </summary>
        /// <returns></returns>
        [HttpGet(nameof(GetAvailableRoles))]
        public async Task<IEnumerable<string>> GetAvailableRoles()
        {
            return await _adminModule.ExecuteQueryAsync(new GetAvailableRolesQuery());
        }

        /// <summary>
        /// Add a new node. Either it could be a Data node or an Author node.
        /// </summary>
        /// <param name="treeId"> Tree Id(Guid) of the respective tree.</param>
        /// <param name="subTreeId">SubTree Id(Guid) of the respective tree.</param>
        /// <param name="nodeName">Name of the node</param>
        /// <param name="nodeType">Whether it's Author node or Data node.
        /// 0 denotes Author node, 1 denotes Data node</param>
        /// <param name="nodeLevel">Node level of the adding Data node.
        /// Zero for the Author node.</param>
        /// <param name="nodeIndex">Adding node position index</param>
        /// <param name="leafNode">Whether it's leaf node or not.
        /// 0 denotes no. 1 denotes yes</param>
        /// <param name="approvalLabel">Approval label of an Author node</param>
        /// <param name="rejectLabel">Reject label of an Author node</param>
        /// <param name="nodeMappingLevel">Node Mapping Level for PushDown SubTree</param>
        /// <param name="nodeRoleModels">Added roles of an Author node</param>
        /// <returns></returns>
        [HttpPost(nameof(AddNode))]
        public async Task<ActionResult> AddNode(Guid treeId, Guid subTreeId, string nodeName, NodeTypeEnum nodeType,
            int nodeLevel, int nodeIndex, LeafNodeEnum leafNode, string approvalLabel, string rejectLabel,
            int nodeMappingLevel, List<NodeRoleModel> nodeRoleModels)
        {
            _ = await _adminModule.ExecuteCommandAsync(
                    new AddNodeCommand(treeId, subTreeId, nodeName, nodeType, nodeLevel,
                        nodeIndex, leafNode, approvalLabel, rejectLabel, nodeMappingLevel, nodeRoleModels));
            return Ok(new { success = true, successMessage = "Add Node Successfully" });

        }

        /// <summary>
        /// To update an Author node.
        /// </summary>
        /// <param name="treeId"> Tree Id(Guid) of the respective tree.</param>
        /// <param name="subTreeId">SubTree Id(Guid) of the respective tree.</param>
        /// <param name="nodeId">Node Id(Guid) of the respective tree</param>
        /// <param name="nodeName">Name of the node</param>
        /// <param name="approvalLabel">Approval label of an Author node</param>
        /// <param name="rejectLabel">Reject label of an Author node</param>
        /// <param name="nodeMappingLevel">Node Mapping Level for PushDown SubTree</param>
        /// <param name="nodeRoleModels">Added roles of an Author node</param>
        /// <returns></returns>
        [HttpPut(nameof(UpdateAuthorNode))]
        public async Task<ActionResult> UpdateAuthorNode(Guid treeId, Guid subTreeId, Guid nodeId,
            string nodeName, string approvalLabel, string rejectLabel, int nodeMappingLevel, List<NodeRoleModel> nodeRoleModels)
        {

            _ = await _adminModule.ExecuteCommandAsync(
                new UpdateAuthorNodeCommand(treeId, subTreeId, nodeId,
                    nodeName, approvalLabel, rejectLabel, nodeMappingLevel, nodeRoleModels));
            return Ok(new { success = true, successMessage = "Update Author Node Successfully" });

        }

        /// <summary>
        /// To update a Data node.
        /// </summary>
        /// <param name="treeId"> Tree Id(Guid) of the respective tree.</param>
        /// <param name="subTreeId">SubTree Id(Guid) of the respective tree.</param>
        /// <param name="nodeId">Node Id(Guid) of the respective tree</param>
        /// <param name="nodeName">Name of the node</param>
        /// <param name="nodeLevel">Node Level of the respective node</param>
        /// <returns></returns>
        [HttpPut(nameof(UpdateDataNode))]
        public async Task<ActionResult> UpdateDataNode(Guid treeId, Guid subTreeId, Guid nodeId,
            string nodeName, int nodeLevel)
        {
            _ = await _adminModule.ExecuteCommandAsync(
                    new UpdateDataNodeCommand(treeId, subTreeId, nodeId, nodeName, nodeLevel));
            return Ok(new { success = true, successMessage = "Update Data Node Successfully" });

        }

        /// <summary>
        /// To publish a tree.
        /// </summary>
        /// <param name="treeId">Publishing tree Id(Guid)</param>
        /// <returns></returns>
        [HttpPost(nameof(Publish))]
        public async Task<ActionResult> Publish(Guid treeId)
        {
            _ = await _adminModule.ExecuteCommandAsync(new PublishTreeCommand(treeId));
            return Ok(new { success = true, successMessage = "Pusblished Successfully" });

        }

        /// <summary>
        /// To reset a tree.
        /// </summary>
        /// <param name="treeId">Resetting tree Id(Guid)</param>
        /// <returns></returns>
        [HttpPost(nameof(Reset))]
        public async Task<ActionResult> Reset(Guid treeId)
        {

            _ = await _adminModule.ExecuteCommandAsync(new ResetTreeCommand(treeId));
            return Ok(new { success = true, successMessage = "Reset Successfully" });


        }

        /// <summary>
        /// To remove a node. It could be a Data node or an Author node.
        /// </summary>
        /// <param name="treeId">Tree Id(Guid) of the respective node.</param>
        /// <param name="subTreeId">Sub Tree Id(Guid) of the respective node.</param>
        /// <param name="nodeIndex">Removing node index.</param>
        /// <returns></returns>
        [HttpDelete(nameof(RemoveNode))]
        public async Task<ActionResult> RemoveNode(Guid treeId, Guid subTreeId, int nodeIndex)
        {
            _ = await _adminModule.ExecuteCommandAsync(
                    new RemoveNodeCommand(treeId, subTreeId, nodeIndex));
            return Ok(new { success = true, successMessage = "Remove Node Successfully" });

        }

        /// <summary>
        /// To get approval trees
        /// </summary>
        /// <returns></returns>
        [HttpGet(nameof(GetApprovalTrees))]
        public async Task<IEnumerable<ApprovalTreesDto>> GetApprovalTrees()
        {
            return await _adminModule.ExecuteQueryAsync(new GetApprovalTreesQuery());
        }

        /// <summary>
        /// To get author node by subtree
        /// </summary>
        /// <returns></returns>
        [HttpGet(nameof(GetAuthorNodeBySubtree))]
        public async Task<IEnumerable<GetAuthorNodeDto>> GetAuthorNodeBySubtree(
            Guid subTreeId)
        {
            return await _adminModule.ExecuteQueryAsync(new GetAuthorNodeQuery(
                subTreeId));
        }

        [HttpGet(nameof(GetPermissionsByRole))]
        public async Task<IEnumerable<PermissionDto>> GetPermissionsByRole()
        {
            return await _adminModule
                .ExecuteQueryAsync(
                    new GetPermissionsByRoleQuery(_executionContextAccessor.UserRole));
        }

        [HttpGet(nameof(GetAvailableMappingNodes))]
        public async Task<IEnumerable<MappingNodeDto>> GetAvailableMappingNodes(int nodeIndex, AddNewNodeEnum addNewNodeEnum)
        {
            return await _adminModule
                .ExecuteQueryAsync(
                    new GetAvailableMappingNodesQuery(nodeIndex, addNewNodeEnum));
        }
    }
}
