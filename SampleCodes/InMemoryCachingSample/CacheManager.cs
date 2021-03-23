using Dapper;
using IplanHEE.BuildingBlocks.Application.Data;
using IplanHEE.BuildingBlocks.Domain.CacheStores;
using IplanHEE.BuildingBlocks.Domain.PersistenceContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IplanHEE.BuildingBlocks.Infrastructure.CacheStores
{
    public class CacheManager : ICacheManager
    {
        private readonly ICacheStore _cacheStore;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public CacheManager(ICacheStore cacheStore, ISqlConnectionFactory sqlConnectionFactory)
        {
            _cacheStore = cacheStore;
            _sqlConnectionFactory = sqlConnectionFactory;
        }

        public async Task Add<TItem>(ICacheKey<TItem> key) where TItem : class
        {
            _cacheStore.Add(await this.GetCachingData(key), key);
        }

        public async Task<List<TItem>> Get<TItem>(ICacheKey<TItem> key) where TItem : class
        {
            List<TItem> result = _cacheStore.Get(key);
            if (result == null)
            {
                await this.Add(key);
            }

            return _cacheStore.Get(key);
        }

        public async Task Refresh<TItem>(ICacheKey<TItem> key) where TItem : class
        {
            _cacheStore.Remove(key);
            await this.Add(key);
        }
        public Task RemovePermission<TItem>(ICacheKey<TItem> key) where TItem : class
        {
            _cacheStore.Remove(key);
            return Task.CompletedTask;
        }
        private string GetSqlCommand(string keyName)
            => keyName switch
            {
                DatabaseTableNames.CostCentreHierarchyTable => GetCostCentreHierarchyCommand(),
                DatabaseTableNames.ApprovalStatusTable => GetApprovalStatusCommand(),
                DatabaseTableNames.DropdownMappingTable => GetDropdownMappingCommand(),
                DatabaseTableNames.ApprovalRoleTable => GetNodeRolesCommand(),
                DatabaseTableNames.ScenariosTable => GetScenariosCommand(),
                DatabaseTableNames.GlCodesUiTable => GetGlCodesUiCommand(),
                _ => GetUserCostCentrePermissionCommand(
                        keyName.Split(new string[] { DomainKeyValueProperties.UserNameAndRoleSplittingKey }, StringSplitOptions.None)[0],
                    keyName.Split(new string[] { DomainKeyValueProperties.UserNameAndRoleSplittingKey }, StringSplitOptions.None)[1])
            };

        private async Task<dynamic> GetCachingData<TItem>(ICacheKey<TItem> key) where TItem : class
        {
            var connection = _sqlConnectionFactory.GetEnterpriseHeeOpenConnection();
            var sql = GetSqlCommand(key.CacheKey);
            if (!key.CacheKey.Contains(DomainKeyValueProperties.UserNameAndRoleSplittingKey))
                return (await connection.QueryAsync<TItem>(sql)).AsList();

            var costCentreHierarchy = await this.Get(new CacheCostCentreHierarchyKey());
            var userCostCentrePermissions = await connection.QueryAsync<UserCostCentrePermissionModel>(sql);

            return costCentreHierarchy
                .Join(userCostCentrePermissions
                    , cc => cc.CostCentre
                    , ac => ac.CostCentre
                    , (cc, ac) =>
                        new CacheRoleBasedUserHierarchicalPermissionDto
                        {
                            CostCentre = cc.CostCentre,
                            CostCentreName = cc.CostCentreName,
                            Region = cc.Region,
                            Institute = cc.Institute,
                            DeliveryType = cc.DeliveryType,
                            Group = cc.Group,
                            ActivityLevel1 = cc.ActivityLevel1,
                            ActivityLevel2 = cc.ActivityLevel2,
                            ActivityLevel3 = cc.ActivityLevel3,
                            StartDate = ac.StartDate,
                            EndDate = ac.EndDate

                        })
                .Where(cch =>
                    (cch.EndDate == default || cch.StartDate <= DateTime.Now)
                    && (cch.StartDate == default || cch.EndDate >= DateTime.Now))
                .ToList();

        }

        private string GetCostCentreHierarchyCommand()
            => $"SELECT [cost_centre] AS [{nameof(CacheCostCentreHierarchyDto.CostCentre)}] " +
                        $",[cost_centre_name] AS [{nameof(CacheCostCentreHierarchyDto.CostCentreName)}] " +
                        $",[region] AS [{nameof(CacheCostCentreHierarchyDto.Region)}] " +
                        $",[institute] AS [{nameof(CacheCostCentreHierarchyDto.Institute)}] " +
                        $",[delivery_type] AS [{nameof(CacheCostCentreHierarchyDto.DeliveryType)}] " +
                        $",[group] AS [{nameof(CacheCostCentreHierarchyDto.Group)}] " +
                        $",[activity_level_1] AS [{nameof(CacheCostCentreHierarchyDto.ActivityLevel1)}] " +
                        $",[activity_level_2] AS [{nameof(CacheCostCentreHierarchyDto.ActivityLevel2)}] " +
                        $",[activity_level_3] AS [{nameof(CacheCostCentreHierarchyDto.ActivityLevel3)}] " +
                        $",[sap_cost_center_node] AS [{nameof(CacheCostCentreHierarchyDto.SapCostCentreNode)}] " +
                        $",[update_on] AS [{nameof(CacheCostCentreHierarchyDto.UpdateOn)}] " +
                        $",[additional_Column_01] AS [{nameof(CacheCostCentreHierarchyDto.AdditionalColumn1)}] " +
                        $",[additional_Column_02] AS [{nameof(CacheCostCentreHierarchyDto.AdditionalColumn2)}] " +
                        $",[additional_Column_03] AS [{nameof(CacheCostCentreHierarchyDto.AdditionalColumn3)}] " +
                        $",[additional_Column_04] AS [{nameof(CacheCostCentreHierarchyDto.AdditionalColumn4)}] " +
                        $",[additional_Column_05] AS [{nameof(CacheCostCentreHierarchyDto.AdditionalColumn5)}] " +
                        $",[primary_cost_centre] AS [{nameof(CacheCostCentreHierarchyDto.PrimaryCostCentre)}] " +
                    "FROM [CostCentreHierarchy]";


        private string GetApprovalStatusCommand()
            => "SELECT [ApprovalLevel] " +
                       ",[NodeMappingLevel] " +
                       ",[NodeName] " +
                       ",[DataNodeName] " +
                       ",[ApprovalStatusLabel] " +
                       ",[RejectStatusLabel] " +
                       ",[IsSubmitNode] " +
                       ",[EmployeeId] " +
                       ",[UpdateOn] " +
                       ",[NodeId] " +
                       ",[SubtreeId] " +
                       ",[SubTreeName] " +
                       "FROM [ApprovalTree].[ApprovalStatus]";


        private string GetDropdownMappingCommand()
            => $"SELECT [dropdown_name] AS [{nameof(CacheDropdownMappingDto.DropdownName)}] " +
                       $",[dropdown_label] AS [{nameof(CacheDropdownMappingDto.DropdownLabel)}] " +
                       $",[value_from] AS [{nameof(CacheDropdownMappingDto.ValueFrom)}] " +
                       $",[screen_name] AS [{nameof(CacheDropdownMappingDto.ScreenName)}] " +
                       $",[order_by] AS [{nameof(CacheDropdownMappingDto.OrderBy)}] " +
                       $",[table_name] AS [{nameof(CacheDropdownMappingDto.TableName)}] " +
                       $",[is_active] AS [{nameof(CacheDropdownMappingDto.IsActive)}] " +
                       "FROM [dbo].[DropdownMapping]";

        private string GetNodeRolesCommand()
            => "SELECT [Role] " +
                   ",[IsViewOnly] " +
                   ",[IsRemoved] " +
                   ",[NodeId] " +
               "FROM [ApprovalTree].[NodeRoles]";

        private string GetUserCostCentrePermissionCommand(string userName, string role)
            => $"SELECT employee_id AS [{nameof(UserCostCentrePermissionModel.EmployeeId)}], " +
                   $"[role] AS[{nameof(UserCostCentrePermissionModel.Role)}], " +
                   $"email_address AS[{nameof(UserCostCentrePermissionModel.Email)}], " +
                   $"cost_centre AS[{nameof(UserCostCentrePermissionModel.CostCentre)}], " +
                   $"start_date AS[{nameof(UserCostCentrePermissionModel.StartDate)}], " +
                   $"end_date AS[{nameof(UserCostCentrePermissionModel.EndDate)}], " +
                   $"[group] AS[{nameof(UserCostCentrePermissionModel.Group)}], " +
                   $"delivery_type AS[{nameof(UserCostCentrePermissionModel.DeliveryType)}] " +
                   "FROM UserCostCentrePermission " +
               "WHERE employee_id = '" + userName + "' AND [role] = '" + role + "' ";

        private string GetScenariosCommand()
            => $"SELECT sc.[scenario_id] AS [{nameof(CacheScenariosDto.ScenarioId)}] " +
                       $",sc.[year_id] AS [{nameof(CacheScenariosDto.YearId)}] " +
                       $",sc.[scenario_name] AS [{nameof(CacheScenariosDto.ScenarioName)}] " +
                       $",sc.[scenario_status] AS [{nameof(CacheScenariosDto.ScenarioStatus)}] " +
                       $",sc.[is_active] AS [{nameof(CacheScenariosDto.IsActive)}] " +
                       $",sc.[target_type_id] AS [{nameof(CacheScenariosDto.TargetTypeId)}] " +
                       $",sc.[base_budget_scenario_id] AS [{nameof(CacheScenariosDto.BaseBudgetScenarioId)}] " +
                       $",sc.[base_scenario_id] AS [{nameof(CacheScenariosDto.BaseScenarioId)}] " +
                       $",sc.[purging_status] AS [{nameof(CacheScenariosDto.PurgingStatus)}] " +
                       $",sc.[purging_complete_on] AS [{nameof(CacheScenariosDto.PurgingCompleteOn)}] " +
                       $",sc.[purging_create_on] AS [{nameof(CacheScenariosDto.PurgingCreateOn)}] " +
                       $",sc.[budget_calculator_status] AS [{nameof(CacheScenariosDto.BudgetCalculatorStatus)}] " +
                       $",sc.[budget_calculator_status_label] AS [{nameof(CacheScenariosDto.BudgetCalculatorStatusLabel)}] " +
                       $",sc.[cube_scenario] AS [{nameof(CacheScenariosDto.CubeScenario)}] " +
                       $",sc.[upload_lock_status] AS [{nameof(CacheScenariosDto.ScenarioUploadLockStatus)}] " +
                       $",sc.[budget_calculator_update_on] AS [{nameof(CacheScenariosDto.BudgetCalculatorUpdateOn)}] " +
                       $",y.[year_name] AS [{nameof(CacheScenariosDto.YearName)}] " +
                       $",y.[financial_year_name] AS [{nameof(CacheScenariosDto.FinancialYearName)}] " +
                       $",y.[upload_lock_status] AS [{nameof(CacheScenariosDto.YearUploadLockStatus)}] " +
                       "FROM [Scenarios] sc INNER JOIN" +
                       "[Years] y ON sc.[year_id] = y.[year_id]";

        private string GetGlCodesUiCommand()
            => $"SELECT [gl_code_id] AS [{nameof(CacheGlCodesUiDto.GlCodeId)}] " +
                       $",[cost_type] AS [{nameof(CacheGlCodesUiDto.CostType)}] " +
                       $",[gl_code] AS [{nameof(CacheGlCodesUiDto.GlCode)}] " +
                       $",[gl_description] AS [{nameof(CacheGlCodesUiDto.GlDescription)}] " +
                       $",[on_cost_pct] AS [{nameof(CacheGlCodesUiDto.OnCostPct)}] " +
                       $",[gl_rate] AS [{nameof(CacheGlCodesUiDto.GlRate)}] " +
                       $",[region] AS [{nameof(CacheGlCodesUiDto.Region)}] " +
                       $",[fund] AS [{nameof(CacheGlCodesUiDto.Fund)}] " +
                       $",[gl_level_1] AS [{nameof(CacheGlCodesUiDto.GlLevel1)}] " +
                       $",[gl_level_2] AS [{nameof(CacheGlCodesUiDto.GlLevel2)}] " +
                       $",[gl_level_3] AS [{nameof(CacheGlCodesUiDto.GlLevel3)}] " +
                       $",[split_pct] AS [{nameof(CacheGlCodesUiDto.SplitPct)}] " +
                       $",[delivery] AS [{nameof(CacheGlCodesUiDto.Delivery)}] " +
                       $",[regional_non_delivery] AS [{nameof(CacheGlCodesUiDto.RegionalNonDelivery)}] " +
                       $",[corporate_non_delivery] AS [{nameof(CacheGlCodesUiDto.CorporateNonDelivery)}] " +
                       $",[product_design] AS [{nameof(CacheGlCodesUiDto.ProductDesign)}] " +
                       "FROM [GlCodesUI]";
    }
}
