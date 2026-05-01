using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using PayrollBackendProject.Infrastructure.Data;
using PayrollBackendProject.IntegrationTests;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Text;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.FullTest;

public class PayRunIntegrationTetsts : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PayRunIntegrationTetsts(CustomWebApplicationFactory factory)
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "super_secret_test_key_1234567890123456");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "test_issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "test_audience");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");

        _client = factory.CreateClient();
        _factory = factory;
    }

    // Create a csv to send as a request
    private MultipartFormDataContent createCsvForRequest()
    {
        // Create a verbatim string that holds the CSV content that will be sent
        string csvRows = @"Provider Name,Applied Date,Patient ID,DOS,CPT,Payment ID,Payment Date,Applied Payments,Applied Adjustments,Desc,Acct no,Payer,Applied By
""FirstNameClinician LastNameClinician, PsyD"",03/04/2026,138088730,12/29/2025,90837,1247658447.0,02/26/2026,-31.88,,Ins Pmt,102,CIGNA,HannahRomero
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
    
    /*
    Test that you can sign up for an account as a clinician, sign up for an account as a backend user, upload a csv 
    file as a back end user, create a pay run, approve a pay run, approve a statement, sign in as the clinician user,
    and retrieve the statement for the given user. 
    */
    [Fact]
    public async Task EndToEndTest()
    {
        // Create scope to access services for the db and for the import job (since not using hangfire)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<IImportJob>();

        // Sign up as the clinician
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/signup", new {
            email = "user@example.com",
            password =  "string",
            firstName = "FirstNameClinician",
            lastName = "LastNameClinician",
            role = "clinician"
        });
        response.EnsureSuccessStatusCode();

        // Sign up as the backend user
        HttpResponseMessage backendResponse = await _client.PostAsJsonAsync("/api/auth/signup", new {
            email = "backend@example.com",
            password =  "backenduser",
            firstName = "FirstNameBackend",
            lastName = "LastNameBackend",
            role = "backend"
        });
        backendResponse.EnsureSuccessStatusCode();
        LoginResponseDTO? backendLoginResponse = await backendResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(backendLoginResponse);
        // Save the token for future requests
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", backendLoginResponse.Token);

        // Get the csv file from the helper function
        MultipartFormDataContent csvFile = createCsvForRequest();

        // Upload the csv as the backend user
        HttpResponseMessage uploadResponse = await _client.PostAsync("/api/import/upload", csvFile);
        backendResponse.EnsureSuccessStatusCode();
        // Extract the Guid from the response
        var batchId = await uploadResponse.Content.ReadFromJsonAsync<Guid>();

        // Process the job manually since there is no hangfire in the test suite
        await job.ProcessBatch(batchId);

        // Create a pay run from the backend user
        HttpResponseMessage payRunResponse = await _client.PostAsJsonAsync("/api/payrun",
            new { StartDate = new DateTime(2025, 11, 1), EndDate = new DateTime(2026, 4, 1) });
        payRunResponse.EnsureSuccessStatusCode();
        PayRunResponseDTO? payRunReponseContent = await payRunResponse.Content.ReadFromJsonAsync<PayRunResponseDTO>();
        Assert.NotNull(payRunReponseContent);

        // Approve the pay run
        HttpResponseMessage approvePayRunResponseMessage = 
            await _client.PostAsync($"/approveRun/{payRunReponseContent.Id}/approve", null);
        approvePayRunResponseMessage.EnsureSuccessStatusCode();

        // Approve the statement
        HttpResponseMessage retrieveAllStatements = 
            await _client.GetAsync($"/api/PayRun/{payRunReponseContent.Id}");
        List<PayStatementDTO>? listOfStatements = await retrieveAllStatements.Content.ReadFromJsonAsync<List<PayStatementDTO>>();
        Assert.NotNull(listOfStatements);
        foreach(PayStatementDTO s in listOfStatements)
        {
            await _client.PostAsync($"/approveStatement/{s.Id}/approve", null);
        }

        // Sign in as the clinician
        HttpResponseMessage clinicianLoginResponseMessage = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "user@example.com",
            password =  "string"
        });
        clinicianLoginResponseMessage.EnsureSuccessStatusCode();
        LoginResponseDTO? clinicianLoginResponse = await clinicianLoginResponseMessage.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(clinicianLoginResponse);
        // Set the token to the client
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", clinicianLoginResponse.Token);

        // Get the clinicians pay statement
        HttpResponseMessage clinicianStatementMessage = await _client.GetAsync("/api/Me/Statements/");
        clinicianLoginResponseMessage.EnsureSuccessStatusCode();
        List<PayStatementDTO>? clinicianPayStatementDTO = await clinicianStatementMessage.Content.ReadFromJsonAsync<List<PayStatementDTO>>();
        Assert.NotNull(clinicianPayStatementDTO);
        Assert.Equal(-31.88m, clinicianPayStatementDTO[0].TotalPayment);
    }
    
}