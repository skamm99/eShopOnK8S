using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Basket.API;
using Basket.API.Infrastructure.Filters;
using Basket.API.Infrastructure.Middlewares;
using Basket.API.Model;
using Basket.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Swagger;

namespace Ordering.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1).AddControllersAsServices();

     //       ConfigureAuthService(services);

            services.Configure<BasketSettings>(Configuration);

            services.AddSingleton<ConnectionMultiplexer>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<BasketSettings>>().Value;
                var configuration = ConfigurationOptions.Parse(settings.ConnectionString, true);

                configuration.ResolveDns = true;

                return ConnectionMultiplexer.Connect(configuration);
            });

            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Info
                {
                    Title = "Basket HTTP API",
                    Version = "v1",
                    Description = "The Basket Service HTTP API",
                    TermsOfService = "Terms Of Service"
                });

                // options.AddSecurityDefinition("oauth2", new OAuth2Scheme
                // {
                //     Type = "oauth2",
                //     Flow = "implicit",
                //     AuthorizationUrl = $"{Configuration.GetValue<string>("IdentityUrlExternal")}/connect/authorize",
                //     TokenUrl = $"{Configuration.GetValue<string>("IdentityUrlExternal")}/connect/token",
                //     Scopes = new Dictionary<string, string>()
                //     {
                //         { "basket", "Basket API" }
                //     }
                // });

                //options.OperationFilter<AuthorizeCheckOperationFilter>();
            });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IBasketRepository, RedisBasketRepository>();
            services.AddTransient<IIdentityService, IdentityService>();

            services.AddOptions();

            //var container = new ContainerBuilder();
            //container.Populate(services);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            app.UseStaticFiles();
            app.UseCors("CorsPolicy");

   //         app.UseHttpsRedirection();

    //        ConfigureAuth(app);

            app.UseMvcWithDefaultRoute();

            app.UseSwagger()
               .UseSwaggerUI(c =>
               {
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Basket.API V1");
                   c.OAuthClientId("basketswaggerui");
                   c.OAuthAppName("Basket Swagger UI");
               });

            app.Run(context => {
            context.Response.Redirect("/swagger");
            return Task.CompletedTask;
        });
        }

        // private void ConfigureAuthService(IServiceCollection services)
        // {
        //     // prevent from mapping "sub" claim to nameidentifier.
        //     JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        //     var identityUrl = Configuration.GetValue<string>("IdentityUrl");

        //     //services.AddAuthentication(options =>
        //     //{
        //     //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //     //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

        //     //}).AddJwtBearer(options =>
        //     //{
        //     //    options.Authority = identityUrl;
        //     //    options.RequireHttpsMetadata = false;
        //     //    options.Audience = "basket";
        //     //});
        // }

        // protected virtual void ConfigureAuth(IApplicationBuilder app)
        // {
        //     if (Configuration.GetValue<bool>("UseLoadTest"))
        //     {
        //         app.UseMiddleware<ByPassAuthMiddleware>();
        //     }

        //     app.UseAuthentication();
        // }
    }
}
