using System;
using System.Collections.Generic;
using System.Text;
using IplanHEE.BuildingBlocks.Domain.CacheStores;
using IplanHEE.BuildingBlocks.Domain.PersistenceContract;

namespace IplanHEE.BuildingBlocks.Infrastructure.CacheStores
{
    public class CacheKeyManager : ICacheKeyManager
    {
        public dynamic GetCacheKey(string tableName)
            => tableName switch
            {
                DatabaseTableNames.CostCentreHierarchyTable => new CacheCostCentreHierarchyKey(),
                DatabaseTableNames.ApprovalStatusTable => new CacheApprovalStatusKey(),
                DatabaseTableNames.DropdownMappingTable => new CacheDropdownMappingKey(),
                DatabaseTableNames.ApprovalRoleTable => new CacheNodeRolesKey(),
                DatabaseTableNames.ScenariosTable => new CacheScenariosKey(),
                DatabaseTableNames.GlCodesUiTable => new CacheGlCodesUiKey(),
                _ => null
            };
    }
}
