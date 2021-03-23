using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using IplanHEE.BuildingBlocks.Application.Data;
using IplanHEE.BuildingBlocks.Domain.UserHierarchicalPermission;
using IplanHEE.CapacityManagement.Application.Adomd;
using IplanHEE.CapacityManagement.Application.Configuration.Queries;

namespace IplanHEE.CapacityManagement.Application.CapacityManagementCostCentre.GetCapacityManagementFteSummaryData
{
    public class GetCapacityManagementFteSummaryQueryHandler : IQueryHandler<GetCapacityManagementFteSummaryQuery, IEnumerable<CapacityManagementFteSummaryDto>>
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly IUserPermission _userPermission;
        private readonly CapacityManagementAdomdManager _adomdManager;
        private readonly CapacityManagementDataMerger _dataMerger;

        public GetCapacityManagementFteSummaryQueryHandler(
            ISqlConnectionFactory sqlConnectionFactory,
            IUserPermission userPermission,
            CapacityManagementAdomdManager adomdManager,
            CapacityManagementDataMerger dataMerger)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
            _userPermission = userPermission;
            _adomdManager = adomdManager;
            _dataMerger = dataMerger;
        }

        public async Task<IEnumerable<CapacityManagementFteSummaryDto>> Handle(GetCapacityManagementFteSummaryQuery request,
          CancellationToken cancellationToken)
        {
            var actualTargetData = await _adomdManager
                .GetCapacityManagementFteSummaryActualTargetData(request);
            var budgetData= await GetCapacityManagementBudgetFteSummary(request);
            return _dataMerger.MergeFteSummaryData(budgetData, actualTargetData);
        }

        private async Task<List<CapacityManagementFteSummaryDto>> GetCapacityManagementBudgetFteSummary(GetCapacityManagementFteSummaryQuery request)
        {
            var connection = _sqlConnectionFactory.GetEnterpriseHeeOpenConnection();

            var cCTableDataType = await _userPermission.GetCostCentreDataTable(request.EmployeeId,
                request.ApprovalRole, request.DeliveryType, request.BudgetFor);

            var parameter = new DynamicParameters();
            parameter.Add("@pEmployeeId", request.EmployeeId, DbType.String);
            parameter.Add("@pScenarioId", request.ScenarioId, DbType.Int32);
            parameter.Add("@pRegion", request.Region, DbType.String);
            parameter.Add("@pInstitute", request.Institute, DbType.String);
            parameter.Add("@pGroup", request.Group, DbType.String);
            parameter.Add("@pActivityLevel1", request.ActivityLevel1, DbType.String);
            parameter.Add("@pActivityLevel2", request.ActivityLevel2, DbType.String);
            parameter.Add("@pActivityLevel3", request.ActivityLevel3, DbType.String);
            parameter.Add("@pCostCentre", request.CostCentre, DbType.String);
            parameter.Add("@pApprovalRole", request.ApprovalRole, DbType.String);
            parameter.Add("@pTabName", request.TabName, DbType.String);
            parameter.Add("@pDeliveryType", request.DeliveryType ?? string.Empty, DbType.String);
            parameter.Add("@pBudgetFor", request.BudgetFor ?? string.Empty, DbType.String);
            if (cCTableDataType.Rows.Count <= 100)
            {
                parameter.Add("@pCostCentreDataTable",
                    cCTableDataType.AsTableValuedParameter("CustomDataType.CCTableDataType"));
            }

            const string sql = "procGetListOfCapacityManagementResourcesSummary";

            return (await connection.QueryAsync<CapacityManagementFteSummaryDto>(
                sql, parameter, commandType: CommandType.StoredProcedure)).AsList();
        }
    }
}
