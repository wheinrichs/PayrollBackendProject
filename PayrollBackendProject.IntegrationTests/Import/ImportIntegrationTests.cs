namespace PayrollBackendProject.IntegrationTests;

public class ImportIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    /*
    Upload creates a batch and can be retrieved. Line items look good and is processed correctly.
    */

    /*
    Cannot upload without a valid token
    */

    /*
    Same file being uploaded twice does not duplicate entries and returns the original batch ID
    */

    /*
    Getting unresolved clinician payments returns the correct list. Resolving clinicians then removes from this list
    */
}