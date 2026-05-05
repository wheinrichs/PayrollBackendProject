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
using PayrollBackendProject.Domain.Entity;
using Microsoft.EntityFrameworkCore;

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

    private async Task<LoginResponseDTO?> SignupForTestAsAdmin()
    {
        var signUpRequest = new SignUpRequestDTO($"new_{Guid.NewGuid()}@test.com", "Password123!", "A", "B", "admin");

        var signUpResponse = await _client.PostAsJsonAsync("/api/auth/signup", signUpRequest);
        signUpResponse.EnsureSuccessStatusCode();

        var loginRequest = new LoginRequestDTO(signUpRequest.Email, signUpRequest.Password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
        return loginResult;
    }

    private async Task<LoginResponseDTO?> SignupForTestAsClinician()
    {   
        var loginRequest = new SignUpRequestDTO($"new_{Guid.NewGuid()}@test.com", "Password123!", "A", "B", "clinician");

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
        var loginResult = await SignupForTestAsAdmin();
        // Attach the token to the requests with this client
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token); 
        
        // Create scope to access services for the db and for the import job (since not using hangfire)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<IImportJob>();
        // Clear any existing db items out
        db.PaymentLineItems.RemoveRange(db.PaymentLineItems);
        db.ImportBatches.RemoveRange(db.ImportBatches);
        await db.SaveChangesAsync();

        // Create the file for the request
        var content = createCsvForRequest();

        // Send the file and wait for the response, ensure the response is successful
        var response = await _client.PostAsync("/api/import/upload", content);
        response.EnsureSuccessStatusCode();

        // Extract the Guid from the response
        var batchId = await response.Content.ReadFromJsonAsync<Guid>();

        Assert.NotEqual(Guid.Empty, batchId);

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
        var loginResult = await SignupForTestAsClinician();
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
    [Fact]
    public async Task Upload_MutipleOfSameFile_ReturnsSameBatch()
    {
        var loginResult = await SignupForTestAsAdmin();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        var content = createCsvForRequest();

        // Send the file for the first time and wait for a response
        var response = await _client.PostAsync("/api/import/upload", content);
        var response2 = await _client.PostAsync("/api/import/upload", content);

        // Assert that both batches went through and returned the same batch id
        Guid responseGuid = await response.Content.ReadFromJsonAsync<Guid>();
        Guid responseGuid2 = await response2.Content.ReadFromJsonAsync<Guid>();
        response.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        Assert.Equal(responseGuid, responseGuid2);

    }

    /*
    Getting unresolved clinician payments returns the correct list. Resolving clinicians then removes from this list
    */
    [Fact]
    public async Task Resolve_ResolvingMissingClinicians_RemovesFromList()
    {
        var loginResult = await SignupForTestAsAdmin();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);
        var content = createCsvForRequest();

        // Retrieve the Db and the job processor
        var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
        var jobProcessor = scope.ServiceProvider.GetRequiredService<IImportJob>();

        // Clear any existing payments or batches out of the database
        db.PaymentLineItems.RemoveRange(db.PaymentLineItems);
        db.ImportBatches.RemoveRange(db.ImportBatches);
        await db.SaveChangesAsync();

        // Send the file and wait for the response, ensure the response is successful
        var response = await _client.PostAsync("/api/import/upload", content);
        response.EnsureSuccessStatusCode();

        // Extract the Guid from the response
        var batchId = await response.Content.ReadFromJsonAsync<Guid>();

        Assert.NotEqual(Guid.Empty, batchId);

        // Assert - Batch exists
        var batch = await db.ImportBatches.FindAsync(batchId);
        Assert.NotNull(batch);
        Assert.Equal(ImportStatusEnum.PENDING, batch!.ImportStatus);

        // Act - Manually process batch (since Hangfire is disabled)
        await jobProcessor.ProcessBatch(batchId);

        // Validate that there are 4 unresolved clinician items from the first test that was run as apart of this file
        List<PaymentLineItem> unresolvedPayments = await db.PaymentLineItems.Where(p => p.ClinicianId == null).ToListAsync();
        Assert.Equal(4, unresolvedPayments.Count);

        // Assert that retrieving the unresolved payments from the endpoing gets the same 4 payments
        var unresolvedPaymentsEndpointResult = await _client.GetAsync("/api/import/UnresolvedClinicianPayments");
        var unresolvedPaymentsEndpoint = await unresolvedPaymentsEndpointResult.Content.ReadFromJsonAsync<List<PaymentLineItemDTO>>();
        Assert.Equal(4, unresolvedPaymentsEndpoint!.Count);
        foreach(PaymentLineItem payment in unresolvedPayments)
        {
            Assert.Contains(unresolvedPaymentsEndpoint, p => p.Id == payment.Id);
        }

        // Add in two clinicians, resolve clinicians, and rerun the test to ensure the 2 payment line items are resolved
        response = await _client.PostAsJsonAsync("/api/auth/signup", new SignUpRequestDTO("jsmith@example.com", "fakepassword", "Joe", "Smith", "clinician"));
        response.EnsureSuccessStatusCode();
        response = await _client.PostAsJsonAsync("/api/auth/signup", new SignUpRequestDTO("jadams@example.com", "fakepassword", "Jane", "Adams", "clinician"));
        response.EnsureSuccessStatusCode();
        response = await _client.PostAsync("/api/import/ResolveCliniciansForPayments", null);
        response.EnsureSuccessStatusCode();

        unresolvedPaymentsEndpointResult = await _client.GetAsync("/api/import/UnresolvedClinicianPayments");
        unresolvedPaymentsEndpoint = await unresolvedPaymentsEndpointResult.Content.ReadFromJsonAsync<List<PaymentLineItemDTO>>();
        Assert.Equal(2, unresolvedPaymentsEndpoint!.Count);
    }
}