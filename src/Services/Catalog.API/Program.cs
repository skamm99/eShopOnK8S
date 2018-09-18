using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Catalog.API.Infrastructure;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Catalog.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args)
                .MigrateDbContext<CatalogContext>((context, services) =>
                {
                    var env = services.GetService<IHostingEnvironment>();
                    var settings = services.GetService<IOptions<CatalogSettings>>();
                    var logger = services.GetService<ILogger<CatalogContextSeed>>();

                    new CatalogContextSeed()
                    .SeedAsync(context, env, settings, logger)
                    .Wait();

                })
                .Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()                
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot("Pics")
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    var builtConfig = config.Build();

                    var configurationBuilder = new ConfigurationBuilder();

                    //if (Convert.ToBoolean(builtConfig["UseVault"]))
                    //{
                    //    configurationBuilder.AddAzureKeyVault(
                    //        $"https://{builtConfig["Vault:Name"]}.vault.azure.net/",
                    //        builtConfig["Vault:ClientId"],
                    //        builtConfig["Vault:ClientSecret"]);
                    //}

                    configurationBuilder.AddEnvironmentVariables();

                    config.AddConfiguration(configurationBuilder.Build());
                })
                .ConfigureLogging((hostingContext, builder) =>
                {
                    builder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    builder.AddConsole();
                    builder.AddDebug();
                })
                .Build();
    }

    public static class IWebHostExtensions
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
        public static IWebHost MigrateDbContext<TContext>(this IWebHost webHost, Action<TContext, IServiceProvider> seeder) where TContext : DbContext
        {
            using (var scope = webHost.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var logger = services.GetRequiredService<ILogger<TContext>>();

                var context = services.GetService<TContext>();

                try
                {
                    logger.LogInformation($"Migrating database associated with context {typeof(TContext).Name}");

                    var retry = Policy.Handle<SqlException>()
                         .WaitAndRetry(new TimeSpan[]
                         {
                             TimeSpan.FromSeconds(3),
                             TimeSpan.FromSeconds(5),
                             TimeSpan.FromSeconds(8),
                         });

                    retry.Execute(() =>
                    {
                        //if the sql server container is not created on run docker compose this
                        //migration can't fail for network related exception. The retry options for DbContext only 
                        //apply to transient exceptions.

                        context.Database
                        .Migrate();

                        seeder(context, services);
                    });


                    logger.LogInformation($"Migrated database associated with context {typeof(TContext).Name}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred while migrating the database used on context {typeof(TContext).Name}");
                }
            }

            return webHost;
        }

        //public static IWebHostBuilder UseHealthChecks(this IWebHostBuilder builder, string path)
        //    => UseHealthChecks(builder, path, DefaultTimeout);

        //public static IWebHostBuilder UseHealthChecks(this IWebHostBuilder builder, string path, TimeSpan timeout)
        //{
        //    Guard.ArgumentNotNull(nameof(path), path);
        //    // REVIEW: Is there a better URL path validator somewhere?
        //    Guard.ArgumentValid(!path.Contains("?"), nameof(path), "Path cannot contain query string values.");
        //    Guard.ArgumentValid(path.StartsWith("/"), nameof(path), "Path should start with '/'.");
        //    Guard.ArgumentValid(timeout > TimeSpan.Zero, nameof(timeout), "Health check timeout must be a positive time span.");

        //    builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new HealthCheckStartupFilter(path, timeout)));
        //    return builder;
        //}
    }
}
