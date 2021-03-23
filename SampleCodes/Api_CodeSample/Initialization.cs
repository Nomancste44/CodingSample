using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using IplanHEE.Admin.Infrastructure.Configuration;
using IplanHEE.Api.Authentication;
using IplanHEE.Api.Configuration.Extensions;
using IplanHEE.Api.DependencyRegister;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IplanHEE.Api.Authentication.Authorization;
using IplanHEE.Api.Configuration.ExecutionContext;
using IplanHEE.BuildingBlocks.Application;
using IplanHEE.BuildingBlocks.Application.Data;
using IplanHEE.BuildingBlocks.Domain.CacheStores;
using IplanHEE.BuildingBlocks.Domain.PersistenceContract;
using IplanHEE.BuildingBlocks.Infrastructure;
using IplanHEE.BuildingBlocks.Infrastructure.CacheStores;
using IplanHEE.CapacityManagement.Infrastructure.Configuration;
using IplanHEE.CapacityPlanning.Infrastructure.Configuration;
using IplanHEE.EducationalPlanning.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace IplanHEE.Api
{
    public class Startup
    {
        private readonly IDictionary<string, string> _connectionStrings;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _connectionStrings = new Dictionary<string, string>
            {
                {
                    DomainKeyValueProperties.EnterpriseHee,
                    Configuration.GetConnectionString(DomainKeyValueProperties.EnterpriseHee)
                },
                {
                    DomainKeyValueProperties.EnterpriseHeeDw,
                    Configuration.GetConnectionString(DomainKeyValueProperties.EnterpriseHeeDw)
                },
                {
                    DomainKeyValueProperties.EnterpriseHeeJob,
                    Configuration.GetConnectionString(DomainKeyValueProperties.EnterpriseHeeJob)
                },
                {
                    DomainKeyValueProperties.EnterpriseCube,
                    Configuration.GetConnectionString(DomainKeyValueProperties.EnterpriseCube)
                }
            };
            AuthorizationChecker.CheckAllEndpoints();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMemoryCache,MemoryCache>();
            services.AddSingleton<ICacheStore>(x =>
                new MemoryCacheStore(x.GetService<IMemoryCache>()));
            services.AddSingleton<ICacheManager>(x=> 
                new CacheManager(x.GetService<ICacheStore>(),new SqlConnectionFactory(_connectionStrings)));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();
            ConfigureAuthenticationWithJwt(services);
            services.AddCors();
            services.AddControllers();
            services.AddSwaggerDocumentation();

        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterModule(new DependencyRegisterModule());
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var container = app.ApplicationServices.GetAutofacRoot();

            app.UseCors(builder =>
                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            InitializeModules(container);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwaggerDocumentation();
            //app.UseStatusCodePages(async context => 
            //{
            //    var request = context.HttpContext.Request;
            //    var response = context.HttpContext.Response;
            //    if (response.StatusCode == (int) HttpStatusCode.Unauthorized)
            //    {
            //        response.Redirect("/connect/refresh-token");
            //    }
            //});

            app.UseAuthentication();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void InitializeModules(ILifetimeScope container)
        {
            var httpContextAccessor = container.Resolve<IHttpContextAccessor>();
            var executionContextAccessor = new ExecutionContextAccessor(httpContextAccessor);
            AdminStartup.Initialize(
                _connectionStrings,
                executionContextAccessor,
                container.Resolve<IMemoryCache>(),
                container.Resolve<ICacheManager>());
            CapacityManagementStartup.Initialize(
                _connectionStrings,
                executionContextAccessor,
                container.Resolve<IMemoryCache>(),
                container.Resolve<ICacheManager>());
            EducationalPlanningStartup.Initialize(
                _connectionStrings,
                executionContextAccessor,
                container.Resolve<IMemoryCache>(),
                container.Resolve<ICacheManager>());
            CapacityPlanningStartup.Initialize(
                _connectionStrings,
                executionContextAccessor,
                container.Resolve<IMemoryCache>(),
                container.Resolve<ICacheManager>());
        }

        private static void ConfigureAuthenticationWithJwt(IServiceCollection services)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidAudience = AuthenticationConfig.ValidAudience,
                        ValidIssuer = AuthenticationConfig.ValidIssuer,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(AuthenticationConfig.SecretKey))
                    };
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(HasPermissionAttribute.HasPermissionPolicyName, policyBuilder =>
                {
                    policyBuilder
                        .RequireAuthenticatedUser()
                        .Requirements
                        .Add(new HasPermissionAuthorizationRequirement());
                });
                options.InvokeHandlersAfterFailure = false;
            });
            services.AddScoped<IAuthorizationHandler, HasPermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, HasDataAccessPermissionAuthorizationHandler>();
        }

    }

}
