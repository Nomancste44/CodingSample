using Dapper;
using IplanHEE.Admin.Application.Configuration.Queries;
using IplanHEE.Admin.Domain.SharedLine;
using IplanHEE.BuildingBlocks.Application.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IplanHEE.Admin.Application.ApprovalTree.GetTreeNodes
{
    public class GetTreeNodesQueryHandler : IQueryHandler<GetTreeNodesQuery, IEnumerable<TreeNodesDto>>
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public GetTreeNodesQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }
        public async Task<IEnumerable<TreeNodesDto>> Handle(GetTreeNodesQuery request, CancellationToken cancellationToken)
        {
            var connection = _sqlConnectionFactory.GetEnterpriseHeeOpenConnection();
            string sql = "SELECT  " +
                                   $"T.Id AS [{nameof(TreeNodesDto.TreeId)}], " +
                                   $"T.[Name] AS [{nameof(TreeNodesDto.TreeName)}], " +
                                   $"T.[Label] AS [{nameof(TreeNodesDto.TreeLabel)}], " +
                                   $"ST.Id AS [{nameof(TreeNodesDto.SubTreeId)}], " +
                                   $"ST.[Name] AS [{nameof(TreeNodesDto.SubTreeName)}], " +
                                   $"ST.[Label] AS [{nameof(TreeNodesDto.SubTreeLabel)}], " +
                                   $"ST.IsLeafSubTree AS [{nameof(TreeNodesDto.IsLeafSubTree)}], " +
                                   $"ST.IsPushDown AS [{nameof(TreeNodesDto.IsPushDown)}], " +
                                   $"ST.ParentSubTreeId AS [{nameof(TreeNodesDto.ParentSubTreeId)}], " +
                                   $"N.Id AS [{nameof(TreeNodesDto.NodeId)}], " +
                                   $"N.IsAuthorNode AS [{nameof(TreeNodesDto.IsAuthorNode)}] , " +
                                   $"N.IsLeafNode AS [{nameof(TreeNodesDto.IsLeafNode)}], " +
                                   $"N.[Name] AS [{nameof(TreeNodesDto.NodeName)}], " +
                                   $"N.IsPublish AS [{nameof(TreeNodesDto.IsPublish)}], " +
                                   $"N.NodeLevel AS [{nameof(TreeNodesDto.NodeLevel)}], " +
                                   $"N.NodeIndex AS [{nameof(TreeNodesDto.NodeIndex)}], " +
                                   $"NR.Role AS [{nameof(NodeRoleModel.RoleName)}], " +
                                   $"NR.IsViewOnly AS [{nameof(NodeRoleModel.NodePermission)}] " +
                                   "FROM ApprovalTree.Trees T " +
                               "LEFT JOIN ApprovalTree.SubTrees ST ON T.Id = ST.TreeId " +
                               "LEFT JOIN ApprovalTree.Nodes N ON ST.Id = N.SubTreeId  " +
                                   "AND N.IsPublish = @IsPublished " +
                                "LEFT JOIN ApprovalTree.NodeRoles NR ON N.Id = NR.NodeId " +
                                "WHERE T.Name = @TreeName ORDER BY ST.[Name] DESC, N.NodeIndex";
            return await GetTreeNodes(request, connection, sql);
        }

        private async Task<IEnumerable<TreeNodesDto>> GetTreeNodes(GetTreeNodesQuery request, IDbConnection connection, string sql)
        {
            var treeNodes = new Dictionary<Guid, TreeNodesDto>();
            var result = await connection
                .QueryAsync<TreeNodesDto, NodeRoleModel, TreeNodesDto>(sql,
                    map: (tN, nR) =>
                    {
                        if (!treeNodes.TryGetValue(tN.NodeId, out var aTreeNode))
                        {
                            treeNodes.Add(tN.NodeId, aTreeNode = tN);
                        }

                        if (aTreeNode == null) return null;

                        aTreeNode.NodeRoles ??= new List<NodeRoleModel>();

                        if (nR != null && (!aTreeNode?.NodeRoles
                                            .Any(r => r.RoleName == nR.RoleName
                                                      && r.NodePermission == nR.NodePermission
                                            ) ?? false)
                            )
                        {
                            aTreeNode.NodeRoles.Add(nR);
                        }

                        return aTreeNode;
                    },
                    splitOn: nameof(NodeRoleModel.RoleName),
                    param: new { IsPublished = request.TreeState, request.TreeName }
                );

            return result
                .GroupBy(r => r.NodeId)
                .Select(r => r.First());
        }
    }
}
