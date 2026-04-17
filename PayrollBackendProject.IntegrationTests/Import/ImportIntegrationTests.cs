using System.Net.Http.Headers;
using System.Text;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PayrollBackendProject.Infrastructure.Data;
using PayrollBackendProject.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using Microsoft.AspNetCore.Http;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.IntegrationTests;

public class ImportIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    // Save the client so you can use it to hit endpoints
    private readonly HttpClient _client;
    // Save the factory so you can get access to the DB after the request goes through
    private readonly CustomWebApplicationFactory _factory;

    public ImportIntegrationTests(CustomWebApplicationFactory factory)
    {
        // Specify the JWT token information that is normally present in the environment file
        Environment.SetEnvironmentVariable("Jwt__Key", "super_secret_test_key_1234567890123456");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "test_issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "test_audience");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");

        _client = factory.CreateClient();
        _factory = factory;
    }

    private MultipartFormDataContent createCsvForRequest()
    {
        // Create a verbatim string that holds the CSV content that will be send
        string csvRows = @"Provider Name,Applied Date,Patient ID,DOS,CPT,Payment ID,Payment Date,Applied Payments,Applied Adjustments,Desc,Acct no,Payer,Applied By
""Joe Smith, PsyD"",03/04/2026,138088730,12/29/2025,90837,1247658447.0,02/26/2026,-31.88,,Ins Pmt,102,CIGNA,HannahRomero
""Jane Adams, PhdD"",03/05/2026,87469109,12/30/2025,90837,1247658903.0,03/04/2026,-31.88,,Pat Pmt,100,CIGNA,AlexisKremp
""Henry Trial"",03/06/2026,87495148,01/20/2026,90791,1247658956.0,03/04/2026,-34.21,,Ins Reversal,500,CIGNA,HannahRomero
""Karen Lane"",03/07/2026,87495148,01/26/2026,90837,1247658903.0,03/04/2026,-34.21,,Ins Pmt,102,CIGNA,WinstonHeinrichs";
        
        // Convert that string into a byte array that will be passed as the content
        var fileConent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvRows));

        // Set the content header so it is recognized as a csv file
        fileConent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Wrap it as a file upload
        var content = new MultipartFormDataContent();
        // The second parameter here needs to match the parameter in the API endpoint
        content.Add(fileConent, "file", "CsvPayments.csv");
        return content;
    }

    private async Task<LoginResponseDTO?> LoginForTestAsAdmin()
    {
        var loginRequest = new SignUpRequestDTO("newuser@test.com", "Password123!", "A", "B", "admin");

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/signup", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
        return loginResult;
    }

    private async Task<LoginResponseDTO?> LoginForTestAsClinician()
    {
        var loginRequest = new SignUpRequestDTO("clincian@test.com", "Password123!", "A", "B", "clinician");

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/signup", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
        return loginResult;
    }

    /*
    Upload creates a batch and can be retrieved. Line items look good and is processed correctly.
    */
    [Fact]
    public async Task Upload_CreatesBatch_AndProcessesCorrectly()
    {
        // Login so the requests can go through       
        var loginResult = await LoginForTestAsAdmin();
        // Attach the token to the requests with this client
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token); 

        // Create the file for the request
        var content = createCsvForRequest();

        // Send the file and wait for the response, ensure the response is successful
        var response = await _client.PostAsync("/api/import/upload", content);
        response.EnsureSuccessStatusCode();

        // Extract the Guid from the response
        var batchId = await response.Content.ReadFromJsonAsync<Guid>();

        Assert.NotEqual(Guid.Empty, batchId);

        // Create scope to access services for the db and for the import job (since not using hangfire)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<IImportJob>();

        // Assert - Batch exists
        var batch = await db.ImportBatches.FindAsync(batchId);
        Assert.NotNull(batch);
        Assert.Equal(ImportStatusEnum.PENDING, batch!.ImportStatus);

        // Act - Manually process batch (since Hangfire is disabled)
        await job.ProcessBatch(batchId);

        // Reload batch after processing
        batch = await db.ImportBatches.FindAsync(batchId);

        // Assert - Batch processed correctly
        Assert.NotNull(batch);
        Assert.Equal(ImportStatusEnum.SUCCESS_WITH_ERRORS, batch!.ImportStatus);

        // Assert - Line items created
        var lineItems = db.PaymentLineItems
            .Where(p => p.ImportBatchId == batchId)
            .ToList();

        Assert.Equal(4, lineItems.Count);

        // Validate contents of line items
        Assert.Contains(lineItems, li => li.PaymentAmount == -31.88m);
        Assert.Contains(lineItems, li => li.PaymentAmount == -34.21m);

        // Assert - Batch results populated correctly
        Assert.Equal(4, batch.TotalRows);
        Assert.Equal(4, batch.SuccessfulItems);
        Assert.Equal(0, batch.FailedItems);
    }


    /*
    Cannot upload without a valid admintoken
    */
    [Fact]
    public async Task Upload_CreatesBatch_FailsWithoutAdminToken()
    {
        // Login so the requests can go through       
        var loginResult = await LoginForTestAsClinician();
        // Attach the token to the requests with this client
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token); 

        // Create the file for the request
        var content = createCsvForRequest();

        // Send the file and wait for the response, ensure the response is successful
        var response = await _client.PostAsync("/api/import/upload", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /*
    Same file being uploaded twice does not duplicate entries and returns the original batch ID
    */

    /*
    Getting unresolved clinician payments returns the correct list. Resolving clinicians then removes from this list
    */
}