using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PayrollBackendProject.Infrastructure.Data;


// This class gets created and called because of the interface it implements, and then it can create a PayrollDbContext if it needs to 
public class ClinicianDbContextFactory : IDesignTimeDbContextFactory<ClinicianDbContext>
{
    // this is a required method from the interface that returns the context
    public ClinicianDbContext CreateDbContext(string[] args)
    {
        // This is what DI normally builds for you 
        var optionsBuilder = new DbContextOptionsBuilder<ClinicianDbContext>();


        // Determines what type of database you’re using and how to connect to it
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=payroll;Username=postgres;Password=postgres"
        );
        
        // Creates the context with the optionsBuilder
        return new ClinicianDbContext(optionsBuilder.Options);
    }
}