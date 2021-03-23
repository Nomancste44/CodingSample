using Dapper;
using IplanHEE.Admin.Application.Configuration.Queries;
using IplanHEE.Admin.Domain.SharedLine;
using IplanHEE.BuildingBlocks.Application.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IplanHEE.Admin.Application.ApprovalTree.GetTreeNodes
{
    public class GetAuthorNodeByIdQueryHandler : IQueryHandler<GetAuthorNodeByIdQuery, AuthorNodeDto>
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public GetAuthorNodeByIdQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }
        public async Task<AuthorNodeDto> Handle(GetAuthorNodeByIdQuery request, CancellationToken cancellationToken)
        {
            var sql = "SELECT " +
                          $"N.Id AS [{nameof(AuthorNodeDto.NodeId)}], " +
                          $"N.Name AS [{nameof(AuthorNodeDto.NodeName)}], " +
                          $"N.ApprovalLabel AS [{nameof(AuthorNodeDto.ApprovalLabel)}], " +
                          $"N.RejectLabel AS [{nameof(AuthorNodeDto.RejectLabel)}], " +
                          $"N.IsPublish AS [{nameof(AuthorNodeDto.IsPublished)}], " +
                          $"N.IsAuthorNode AS [{nameof(AuthorNodeDto.IsAuthorNode)}], " +
                          $"N.IsLeafNode AS [{nameof(AuthorNodeDto.IsLeafNode)}], " +
                          $"N.NodeIndex AS [{nameof(AuthorNodeDto.NodeIndex)}], " +
                          $"N.NodeLevel AS [{nameof(AuthorNodeDto.NodeLevel)}], " +
                          $"NR.Role AS [{nameof(AuthorNodeDto.Role)}], " +
                          $"NR.IsViewOnly AS [{nameof(AuthorNodeDto.IsViewOnly)}] " +
                      "FROM ApprovalTree.Nodes N " +
                      "LEFT JOIN ApprovalTree.NodeRoles NR ON N.Id = NR.NodeId " +
                      "WHERE N.IsAuthorNode = 1 AND N.Id = @NodeId " +
                          "AND N.IsPublish = @IsPublished;" +
                     "SELECT  DISTINCT [role] AvailableRoles  FROM users.UserProfiles";
            var connection = _sqlConnectionFactory.GetEnterpriseHeeOpenConnection();
            using var multi = await connection
                    .QueryMultipleAsync(sql, new { request.NodeId, IsPublished = request.TreeState });

            return GetGroupedRolesAuthorNodeDto(multi.ReadAsync<AuthorNodeDto>().Result.ToList(),
                    multi.ReadAsync<string>().Result.ToList());
        }

        private AuthorNodeDto GetGroupedRolesAuthorNodeDto(IReadOnlyCollection<AuthorNodeDto> authorNodeDtoList, List<string> availableRoles)
        {
            var anAuthorNode = authorNodeDtoList?.FirstOrDefault();
            return new AuthorNodeDto
            {
                NodeId = anAuthorNode?.NodeId ?? default,
                NodeName = anAuthorNode?.NodeName ?? default,
                ApprovalLabel = anAuthorNode?.ApprovalLabel ?? default,
                RejectLabel = anAuthorNode?.RejectLabel ?? default,
                IsPublished = anAuthorNode?.IsPublished ?? default,
                IsAuthorNode = anAuthorNode?.IsAuthorNode ?? default,
                IsLeafNode = anAuthorNode?.IsLeafNode ?? default,
                NodeIndex = anAuthorNode?.NodeIndex ?? default,
                NodeLevel = anAuthorNode?.NodeLevel ?? default,
                NodeRoles = authorNodeDtoList?
                    .Select(n => new NodeRoleModel
                    {
                        RoleName = n.Role,
                        NodePermission = n.IsViewOnly
                            ? NodePermissionTypeEnum.ViewOnly
                            : NodePermissionTypeEnum.ApproveReject
                    }).Where(n => !string.IsNullOrEmpty(n.RoleName)).ToList(),
                AvailableRoles = availableRoles
            };
        }
    }
}
