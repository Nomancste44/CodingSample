using IplanHEE.BuildingBlocks.Domain;
using IplanHEE.BuildingBlocks.Domain.CacheStores;
using IplanHEE.BuildingBlocks.Domain.UserHierarchicalPermission;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using IplanHEE.BuildingBlocks.Domain.PersistenceContract;

namespace IplanHEE.BuildingBlocks.Infrastructure.UserHierarchicalPermission
{
    public class UserPermission : IUserPermission
    {
        private readonly ICacheManager _cacheManager;

        public UserPermission(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public async Task<List<CacheRoleBasedUserHierarchicalPermissionDto>> GetModuleBasedAccessibleCostCentreHierarchy(
            string userId, string role, string deliveryType = default, string budgetFor = default)
        {
            var roleBasedAccessibleCch = await _cacheManager.Get(new CacheRoleBasedUserHierarchicalPermissionKey(userId, role));

            return roleBasedAccessibleCch.Where(cch =>
                (
                    string.IsNullOrEmpty(deliveryType) || cch.DeliveryType == deliveryType
                )
                &&
                (
                    string.IsNullOrEmpty(budgetFor)
                    || (budgetFor == Common.Corporate && cch.Group != "Region" && cch.Group != "Regions")
                    || (budgetFor == Common.Regional && (cch.Group == "Region" || cch.Group == "Regions"))

                )

            ).ToList();
        }

        public async Task<CacheApprovalStatusDto> GetApprovalStatusInfoByAuthorNode(string subTreeName, string authorNode, string role)
        {
            return (await _cacheManager.Get(new CacheApprovalStatusKey()))
                .Join(await _cacheManager.Get(new CacheNodeRolesKey())
                    , aps => new { aps.NodeId, aps.SubTreeName, aps.NodeName, Role = role }
                    , r => new { r.NodeId, SubTreeName = subTreeName, NodeName = authorNode, r.Role }
                    , (aps, r) => aps)
                .SingleOrDefault();
        }
        /// <summary>
        /// It gets Module based Cost Centre Hierarchical filtering dropdown.
        /// </summary>
        /// <param name="screenName">Name of the Screen e.g Delivery, TAFE Corporate</param>
        /// <param name="dataNodeName">DataNode name of the respective AuthorNode e.g region.</param>
        /// <param name="initialDdlNo">Beginning point of the ddl e.g 1, 2, 3</param>
        /// <returns>Returns List of Dropdown mapping</returns>
        public async Task<List<CacheDropdownMappingDto>> GetModuleBasedCostCentreHierarchicalDropdowns(
            string screenName, string dataNodeName, int initialDdlNo = default)
        {
            var dropdownMappings = (await _cacheManager.Get(new CacheDropdownMappingKey()))
                .Where(ddm =>
                    ddm.ScreenName == screenName
                    && DatabaseTableNames.CostCentreHierarchyTable == ddm.TableName)
                .ToList();
            var dataNodeDropdownOrder = dropdownMappings
                .SingleOrDefault(ddm => ddm.ValueFrom == dataNodeName)
                ?.OrderBy;

            return dropdownMappings
                .Where(ddm =>
                    ddm.IsActive
                    && ddm.OrderBy <= dataNodeDropdownOrder
                    && ddm.OrderBy > initialDdlNo)
                .ToList();
        }

        public async Task<bool> IsLocationDropdownActive(string screenName)
        {
            return (await _cacheManager
                    .Get(new CacheDropdownMappingKey()))
                .Any(ddm => ddm.ScreenName == screenName
                            && ddm.TableName == DatabaseTableNames.LocationCodesTable
                            && ddm.IsActive);
        }
        public async Task<DataTable> GetCostCentreHierarchyDataTable(string employeeId, string approvalRole,
            string deliveryType = default, string budgetFor = default)
        {
            var costCentreHierarchyList = await GetModuleBasedAccessibleCostCentreHierarchy(
                employeeId, approvalRole, deliveryType, budgetFor);

            var aDatable = new DataTable();
            aDatable.Columns.AddRange(new DataColumn[]
            {
                new DataColumn { ColumnName = DomainKeyValueProperties.CostCentre,DataType = typeof(string)},
                new DataColumn { ColumnName = DomainKeyValueProperties.CostCentreName,DataType = typeof(string)},
                new DataColumn { ColumnName = DomainKeyValueProperties.Region,DataType = typeof(string)},
                new DataColumn { ColumnName = DomainKeyValueProperties.Institute,DataType = typeof(string)},
                new DataColumn { ColumnName = DomainKeyValueProperties.DeliveryType,DataType = typeof(string)},
                new DataColumn { ColumnName = DomainKeyValueProperties.Group,DataType = typeof(string)},
                new DataColumn { ColumnName = DomainKeyValueProperties.ActivityLevel1,DataType = typeof(string)},
                new DataColumn { ColumnName = DomainKeyValueProperties.ActivityLevel2,DataType = typeof(string)},
                new DataColumn { ColumnName = DomainKeyValueProperties.ActivityLevel3,DataType = typeof(string)},
            });

            costCentreHierarchyList.ForEach(x =>
            {
                aDatable.Rows.Add(x.CostCentre, x.CostCentreName, x.Region,
                    x.Institute, x.DeliveryType, x.Group, x.ActivityLevel1,
                    x.ActivityLevel2, x.ActivityLevel3);
            });
            return aDatable;
        }

        public async Task<DataTable> GetCostCentreDataTable(string employeeId, string approvalRole,
            string deliveryType = default, string budgetFor = default)
        {
            var costCentreHierarchyList = await GetModuleBasedAccessibleCostCentreHierarchy(
                employeeId, approvalRole, deliveryType, budgetFor);

            var aDatable = new DataTable();
            aDatable.Columns.AddRange(new DataColumn[]
            {
                new DataColumn { ColumnName = DomainKeyValueProperties.CostCentre,DataType = typeof(string)},
            });

            costCentreHierarchyList.ForEach(x =>
            {
                aDatable.Rows.Add(x.CostCentre);
            });
            return aDatable;
        }

        public async Task<string> GetDataNodeByAuthorNode(string authorNode, string subTreeName)
        {
            return (await _cacheManager
                    .Get(new CacheApprovalStatusKey()))
                .Single(aps =>
                    aps.NodeName == authorNode && aps.SubTreeName == subTreeName)
                .DataNodeName;

        }
       
    }
}
