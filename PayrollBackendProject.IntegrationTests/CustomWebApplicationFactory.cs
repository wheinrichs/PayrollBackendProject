using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PayrollBackendProject.Infrastructure.Data;
using PayrollBackendProject.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class CustomWebApplicationFactor : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Find the existing ClinicianDbContext that is defined in program and get a reference to it
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ClinicianDbContext>));

            // Remove the existing ClinicianDbContext that you found if it exists
            if (descriptor != null)
                services.Remove(descriptor);

            // Add a replacement context for testing
            services.AddDbContext<ClinicianDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
        });
    }
}