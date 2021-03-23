using System;
using IplanHEE.BuildingBlocks.Application.Data;
using IplanHEE.BuildingBlocks.Application.SharedLine;
using IplanHEE.CapacityManagement.Application.CapacityManagementCostCentre.GetCapacityManagementApprovalListingData;
using IplanHEE.CapacityManagement.Application.CapacityManagementCostCentre.GetCapacityManagementDashboardSummaryData;
using IplanHEE.CapacityManagement.Application.CapacityManagementCostCentre.GetCapacityManagementFteSummaryData;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using IplanHEE.BuildingBlocks.Domain;

namespace IplanHEE.CapacityManagement.Application.Adomd
{
    public class CapacityManagementAdomdDataPreparator : AdomdDataTableColumnManager
    {
        private readonly IAdomdProcessor _adomdProcessor;

        public CapacityManagementAdomdDataPreparator(IAdomdProcessor adomdProcessor)
        {
            _adomdProcessor = adomdProcessor;
        }
        internal CapacityManagementDashboardActualTargetDto GetCapacityManagementTopSummaryPreparedData(DataTable dt, AdomdQueryParameterModel queryParameter)
        {
            return GetFteRenamedDataTable(dt)
                .GetSalaryRenamedDataTable(dt)
                .GetPushStatusRenamedDataTable(dt)
                .GetGroupRenamedDataTable(dt)
                .GetFinancialYearRenamedDataTable(dt)
                .GetScenarioRenamedDataTable(dt)
                .GetRenamedDataTable(dt)
                .AsEnumerable()
                .Select(ts => new
                {
                    FinancialYear = ts[ApplicationKeyValueProperties.FinancialYear].ToString(),
                    ScenarioName = _adomdProcessor.GetFilteredScenarioName(ts[ApplicationKeyValueProperties.ScenarioName].ToString(),
                        ts[ApplicationKeyValueProperties.PushStatus].ToString(),
                        ts[ApplicationKeyValueProperties.FinancialYear].ToString(),
                        queryParameter),
                    Salary = decimal.TryParse(ts[ApplicationKeyValueProperties.Salary].ToString(), out var salary) ? salary : 0,
                    Fte = decimal.TryParse(ts[ApplicationKeyValueProperties.Fte].ToString(), out var fte) ? fte : 0
                }).Select(ts => new
                {
                    TargetSalary =
                        ts.ScenarioName == ApplicationKeyValueProperties.Target
                        && ts.FinancialYear == queryParameter.CurrentYear.ToString()
                            ? ts.Salary
                            : 0M,
                    TargetFte = ts.ScenarioName == ApplicationKeyValueProperties.Target
                                            && ts.FinancialYear == queryParameter.CurrentYear.ToString()
                        ? ts.Fte
                        : 0M,
                    ActualSalary = ts.ScenarioName == ApplicationKeyValueProperties.Actual
                                               && ts.FinancialYear == queryParameter.LastYear.ToString()
                        ? (decimal)ts.Salary
                        : 0M,
                    ActualFte = ts.ScenarioName == ApplicationKeyValueProperties.Actual
                                            && ts.FinancialYear == queryParameter.LastYear.ToString()
                        ? ts.Fte
                        : 0M,
                })
                .GroupBy(ts => new
                {
                    string.Empty
                })
                .Select(ts => new CapacityManagementDashboardActualTargetDto()
                {
                    ActualFte = ts.Sum(a => a.ActualFte),
                    TargetFte = ts.Sum(a => a.TargetFte),
                    ActualSalary = ts.Sum(a => a.ActualSalary),
                    TargetSalary = ts.Sum(a => a.TargetSalary)
                }).SingleOrDefault();
        }

        internal IList<CapacityManagementFteSummaryDto> GetCapacityManagementFteSummaryPreparedData(DataTable dt, AdomdQueryParameterModel queryParameter)
        {
            return GetFteRenamedDataTable(dt)
                .GetSalaryRenamedDataTable(dt)
                .GetPushStatusRenamedDataTable(dt)
                .GetScenarioRenamedDataTable(dt)
                .GetSbiCategory1RenamedDataTable(dt)
                .GetSbiCategory2RenamedDataTable(dt)
                .GetFinancialYearRenamedDataTable(dt)
                .GetRenamedDataTable(dt)
                .AsEnumerable()
                .Select(at => new
                {
                    ScenarioName = _adomdProcessor.GetFilteredScenarioName(
                        at[ApplicationKeyValueProperties.ScenarioName].ToString(),
                        at[ApplicationKeyValueProperties.PushStatus].ToString(),
                        at[ApplicationKeyValueProperties.FinancialYear].ToString(),
                        queryParameter),
                    SbiCategory1 = at[ApplicationKeyValueProperties.SbiCategory1].ToString(),
                    SbiCategory2 = at[ApplicationKeyValueProperties.SbiCategory2].ToString(),
                    Salary = decimal.TryParse(at[ApplicationKeyValueProperties.Salary].ToString(), out var salary)
                        ? salary
                        : 0,
                    Fte = double.TryParse(at[ApplicationKeyValueProperties.Fte].ToString(), out var fte) ? fte : 0
                }).GroupBy(at => new
                {
                    at.SbiCategory1,
                    at.SbiCategory2,
                    at.ScenarioName
                }).Select(at => new CapacityManagementFteSummaryDto
                {
                    Group = at.Key.SbiCategory1,
                    SubGroup = at.Key.SbiCategory2,
                    ActualStaffFte = at.Key.ScenarioName == ApplicationKeyValueProperties.Actual ?
                        at.Sum(a => a.Fte) : 0d,
                    ActualAdjustedFullYearSalary = at.Key.ScenarioName == ApplicationKeyValueProperties.Actual ?
                        at.Sum(a => a.Salary) : 0M,
                    TargetStaffFte = at.Key.ScenarioName == ApplicationKeyValueProperties.Target ?
                        at.Sum(a => a.Fte) : 0d,
                    TargetAdjustedFullYearSalary = at.Key.ScenarioName == ApplicationKeyValueProperties.Target ?
                        at.Sum(a => a.Salary) : 0M

                }).ToList();
        }

        internal IList<CapacityManagementApprovalListingModel> GetCapacityManagementApprovalListingPreparedData(DataTable dt, AdomdQueryParameterModel queryParameter)
        {
            return GetFteRenamedDataTable(dt)
                .GetSalaryRenamedDataTable(dt)
                .GetScenarioRenamedDataTable(dt)
                .GetCategoryRenamedDataTable(dt)
                .GetSbiCategory1RenamedDataTable(dt)
                .GetSbiCategory2RenamedDataTable(dt)
                .GetCchDataNodeRenamedDataTable(dt, queryParameter.CurrentDataNode)
                .GetFinancialYearRenamedDataTable(dt)
                .GetRenamedDataTable(dt)
                .AsEnumerable()
                .Select(apl => new
                {
                    Fte = double.TryParse(apl[ApplicationKeyValueProperties.Fte].ToString(), out var fte)
                        ? fte
                        : default,
                    Salary = double.TryParse(apl[ApplicationKeyValueProperties.Salary].ToString(), out var salary)
                        ? salary
                        : default,
                    Scenario = _adomdProcessor.GetFilteredScenarioName(
                        apl[ApplicationKeyValueProperties.ScenarioName].ToString(),
                        string.Empty,
                        apl[ApplicationKeyValueProperties.FinancialYear].ToString(),
                        queryParameter),
                    Category = apl[ApplicationKeyValueProperties.Category].ToString(),
                    SbiCategory1 = apl[ApplicationKeyValueProperties.SbiCategory1].ToString(),
                    SbiCategory2 = apl[ApplicationKeyValueProperties.SbiCategory2].ToString(),
                    CchDataNode = apl[ApplicationKeyValueProperties.CchDataNode].ToString(),
                    FinancialYear =
                        int.TryParse(apl[ApplicationKeyValueProperties.FinancialYear].ToString(), out var year)
                            ? year
                            : default

                }).Select(apl => new CapacityManagementApprovalListingModel
                {
                    ActualTeachingFte =
                        apl.Scenario == ApplicationKeyValueProperties.Actual
                        && apl.Category == Common.Teacher
                        && apl.SbiCategory1 == Common.Direct
                            ? apl.Fte
                            : default,

                    ActualTeachingSalary =
                        apl.Scenario == ApplicationKeyValueProperties.Actual
                        && apl.Category == Common.Teacher
                        && apl.SbiCategory1 == Common.Direct
                            ? apl.Salary
                            : default,

                    ActualNonTeachingFte =
                        apl.Scenario == ApplicationKeyValueProperties.Actual
                        && apl.Category == Common.Teacher
                        && apl.SbiCategory1 == Common.Direct
                            ? apl.Fte
                            : default,

                    ActualNonTeachingSalary =
                        apl.Scenario == ApplicationKeyValueProperties.Actual
                        && apl.Category == Common.NonTeacher
                        && apl.SbiCategory1 == Common.Direct
                            ? apl.Salary
                            : default,

                    ActualIndirectFte =
                        apl.Scenario == ApplicationKeyValueProperties.Actual
                        && apl.SbiCategory1 == Common.Indirect ? apl.Fte : default,

                    ActualIndirectSalary =
                        apl.Scenario == ApplicationKeyValueProperties.Actual
                        && apl.SbiCategory1 == Common.Indirect ? apl.Salary : default,

                    ActualTotalFte = apl.Scenario == ApplicationKeyValueProperties.Actual ? apl.Fte : default,
                    ActualTotalSalary = apl.Scenario == ApplicationKeyValueProperties.Actual ? apl.Salary : default,

                    TargetTeachingFte =
                        apl.Scenario == ApplicationKeyValueProperties.Target
                        && apl.Category == Common.Teacher
                        && apl.SbiCategory1 == Common.Direct
                            ? apl.Fte
                            : default,

                    TargetTeachingSalary =
                        apl.Scenario == ApplicationKeyValueProperties.Target
                        && apl.Category == Common.Teacher
                        && apl.SbiCategory1 == Common.Direct
                            ? apl.Salary
                            : default,

                    TargetNonTeachingFte =
                        apl.Scenario == ApplicationKeyValueProperties.Target
                        && apl.Category == Common.Teacher
                        && apl.SbiCategory1 == Common.Direct
                            ? apl.Fte
                            : default,

                    TargetNonTeachingSalary =
                        apl.Scenario == ApplicationKeyValueProperties.Target
                        && apl.Category == Common.NonTeacher
                        && apl.SbiCategory1 == Common.Direct
                            ? apl.Salary
                            : default,

                    TargetIndirectFte =
                        apl.Scenario == ApplicationKeyValueProperties.Target
                        && apl.SbiCategory1 == Common.Indirect
                            ? apl.Fte
                            : default,

                    TargetIndirectSalary =
                        apl.Scenario == ApplicationKeyValueProperties.Target
                        && apl.SbiCategory1 == Common.Indirect
                            ? apl.Salary
                            : default,
                    TargetTotalFte = apl.Scenario == ApplicationKeyValueProperties.Target? apl.Fte: default,
                    TargetTotalAdjustedSalary = apl.Scenario == ApplicationKeyValueProperties.Target? apl.Salary: default,
                    CurrentDataNode = apl.CchDataNode

                }).GroupBy(apl => new
                {
                    apl.CurrentDataNode
                }).Select(apl => new CapacityManagementApprovalListingModel
                {
                    CurrentDataNode = apl.Key.CurrentDataNode,
                    TargetTeachingFte = apl.Sum(a => a.TargetTeachingFte),
                    TargetTeachingSalary = apl.Sum(a => a.TargetTeachingSalary),
                    TargetNonTeachingFte = apl.Sum(a => a.TargetNonTeachingFte),
                    TargetNonTeachingSalary = apl.Sum(a => a.TargetNonTeachingSalary),
                    TargetIndirectFte = apl.Sum(a => a.TargetIndirectFte),
                    TargetIndirectSalary = apl.Sum(a => a.TargetIndirectSalary),
                    TargetTotalFte = apl.Sum(a => a.TargetTotalFte),
                    TargetTotalAdjustedSalary = apl.Sum(a => a.TargetTotalAdjustedSalary),

                    ActualTeachingFte = apl.Sum(a => a.ActualTeachingFte),
                    ActualTeachingSalary = apl.Sum(a => a.ActualTeachingSalary),
                    ActualNonTeachingFte = apl.Sum(a => a.ActualNonTeachingFte),
                    ActualNonTeachingSalary = apl.Sum(a => a.ActualNonTeachingSalary),
                    ActualIndirectFte = apl.Sum(a => a.ActualIndirectFte),
                    ActualIndirectSalary = apl.Sum(a => a.ActualIndirectSalary),
                    ActualTotalFte = apl.Sum(a => a.ActualTotalFte),
                    ActualTotalSalary = apl.Sum(a => a.ActualTotalSalary),

                }).ToList();
        }
    }
}
