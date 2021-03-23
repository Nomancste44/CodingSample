using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using FluentValidation;
using IplanHEE.Admin.Domain.SharedLine;
using IplanHEE.BuildingBlocks.Application.Data;
using IplanHEE.BuildingBlocks.Application.SharedLine;
using IplanHEE.BuildingBlocks.Domain.PersistenceContract;

namespace IplanHEE.Admin.Application.ApprovalTree.AddNode
{
    public class AddNodeCommandValidation : AbstractValidator<AddNodeCommand>
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public AddNodeCommandValidation(
            IUniquenessChecker uniquenessChecker,
            ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
            this.RuleFor(c => c.RawTreeId)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("TreeId can't be null")
                .NotEmpty()
                .WithMessage("TreeId can't be empty")
                .Matches(new Regex(ApplicationKeyValueProperties.GuidRegexPattern))
                .WithMessage("TreeId isn't a valid Guid")
                .Must(treeId =>
                    !uniquenessChecker.IsUniqueValue(DatabaseTableNames.TreesTable, DatabaseTableColumnNames.Id,
                        treeId))
                .WithErrorCode(errorCode: "400")
                .WithMessage("{PropertyName} is invalid");

            this.RuleFor(c => c.RawSubTreeId)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("SubTreeId can't be null")
                .NotEmpty()
                .WithMessage("SubTreeId can't be empty")
                .Matches(new Regex(ApplicationKeyValueProperties.GuidRegexPattern))
                .WithMessage("SubTreeId isn't a valid Guid").Must(subTreeId =>
                    !uniquenessChecker.IsUniqueValue(DatabaseTableNames.SubTreesTable, DatabaseTableColumnNames.Id,
                        subTreeId))
                .WithErrorCode(errorCode: "400")
                .WithMessage("{PropertyName} is invalid");

            this.RuleFor(c => c.NodeName)
                .NotNull()
                .WithMessage("{PropertyName} can't be null")
                .NotEmpty()
                .WithMessage("{PropertyName} can't be empty")
                .MaximumLength(255)
                .WithMessage("{PropertyValue} is a length of {TotalLength}." +
                             " {PropertyName} allows {MaxLength}");

            this.When(c => c.NodeType == NodeTypeEnum.AuthorNode, () =>
              {

                  this.RuleFor(c => c.ApprovalLabel)
                      .NotNull()
                      .WithMessage("{PropertyName} can't be null")
                      .NotEmpty()
                      .WithMessage("{PropertyName} can't be empty")
                      .MaximumLength(255)
                      .WithMessage("{PropertyValue} is a length of {TotalLength}." +
                                   " {PropertyName} allows {MaxLength}");

                  this.RuleFor(c => c.RejectLabel)
                      .NotNull()
                      .WithMessage("{PropertyName} can't be null")
                      .NotEmpty()
                      .WithMessage("{PropertyName} can't be empty")
                      .MaximumLength(255)
                      .WithMessage("{PropertyValue} is a length of {TotalLength}." +
                                   " {PropertyName} allows {MaxLength}");

                  this.RuleForEach(c => c.NodeRoleModels)
                      .ChildRules(roles =>
                          roles.RuleFor(role => role.RoleName)
                              .Cascade(CascadeMode.Stop)
                              .NotNull()
                              .OverridePropertyName("NodeRoleModels")
                              .WithErrorCode(errorCode: HttpStatusCode.BadRequest.ToString())
                              .WithMessage("Roles value can't be empty")
                              .NotEmpty()
                              .WithErrorCode(errorCode: HttpStatusCode.BadRequest.ToString())
                              .WithMessage("Roles value can't be null"));

                  this.When(c => c.NodeRoleModels.Count > 0, () =>
                  {
                      this.RuleForEach(c => c.NodeRoleModels)
                          .Must((command, role) => command
                              .NodeRoleModels
                              .Count(nr => nr.RoleName == role.RoleName) == 1)
                          .WithMessage("{PropertyValue} is duplicate");
                  });

                  this.When(c => IsPushDownSubTree(c.RawSubTreeId), () =>
                  {
                      this.RuleFor(c => c.NodeMappingLevel)
                          .Cascade(CascadeMode.Stop)
                          .GreaterThan(0)
                          .WithMessage("{PropertyName} can't be 0");
                  });
              });

            this.RuleFor(c => c.NodeIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("{PropertyValue} is less than {ComparisonValue}.");

            
        }

        private bool IsPushDownSubTree(string rawSubTreeId)
        {
            var connection = _sqlConnectionFactory.GetEnterpriseHeeOpenConnection();
            const string sql = "SELECT COUNT(*) FROM ApprovalTree.SubTrees ST " +
                      "WHERE ST.Id = @Id AND ST.IsPushDown = 1";
            return connection.QuerySingle<int>(sql, new {Id = Guid.Parse(rawSubTreeId)}) == 1;
        }
    }
}
