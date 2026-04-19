using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PayrollBackendProject.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PayrollBackendProject.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore.Storage;

namespace PayrollBackendProject.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly InMemoryDatabaseRoot _dbRoot = new();
    private readonly string _dbName = Guid.NewGuid().ToString();


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Remove hangfire from the integration tests so tests run quickly 
            services.RemoveAll<IHostedService>();
            // Replace this service with a custom one so postgres isn't called on enqueue
            services.AddScoped<IBackgroundJobService, TestBackgroundJobService>();

            // Find the existing ClinicianDbContext that is defined in program and get a reference to it
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ClinicianDbContext>));

            // Remove the existing ClinicianDbContext that you found if it exists
            if (descriptor != null)
                services.Remove(descriptor);

            // Add a replacement context for testing
            services.AddDbContext<ClinicianDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName, _dbRoot);
            });
        });
    }
}