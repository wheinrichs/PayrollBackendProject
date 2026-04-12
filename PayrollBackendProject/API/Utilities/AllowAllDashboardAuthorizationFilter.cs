using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace PayrollBackendProject.API.Utilities
{
    public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        // Temporary class to define the authorization level for the hangfire dashboard. Remove in production
        // TODO remove this in production 
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}
