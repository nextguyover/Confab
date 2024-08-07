using System.Data.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Confab.Data;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Confab.Tests.MockDependencies;
using Confab.Services.Interfaces;

namespace Confab.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DataContext>));
            services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            services.Remove(dbConnectionDescriptor);

            services.AddSingleton<DbConnection>(container =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                return connection;
            });

            services.AddDbContext<DataContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            });

            services.Replace(ServiceDescriptor.Scoped<IEmailService, MockEmailService>());
        });

        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var projectDir = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(projectDir, "appsettings.Tests.json");

        builder.ConfigureHostConfiguration(config =>
        {
            config.AddJsonFile(configPath);
        });

        return base.CreateHost(builder);
    }
}