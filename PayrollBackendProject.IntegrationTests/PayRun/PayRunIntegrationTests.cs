namespace PayrollBackendProject.IntegrationTests;

public class PayRunIntegrationTetsts : IClassFixture<CustomWebApplicationFactory>
{
    /*
    Test that generating a pay run onyl retrieves the payments in the given range and creates the correct statements.
    Test that the statements are grouped for the right clinicians and the totals are correct.
    */

    /*
    Cannot generate a pay run without a valid token
    */

    /*
    Test that approving a pay run updates its status and that approving a pay statement updates its status. 
    */

    /*
    Test that modifying a payment line item after its been added to a statement does not modify the statement
    */
    
    /*
    Test that when a clinician is logged in and they request pay statements it only returns their pay statement. 
    */
}