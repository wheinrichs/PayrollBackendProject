using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PayrollBackendProject.Infrastructure.Data;
using PayrollBackendProject.Domain.Entity;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using Microsoft.AspNetCore.Http;

namespace PayrollBackendProject.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    // Save the client so you can use it to hit endpoints
    private readonly HttpClient _client;
    // Save the factory so you can get access to the DB after the request goes through
    private readonly CustomWebApplicationFactory _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        // Specify the JWT token information that is normally present in the environment file
        Environment.SetEnvironmentVariable("Jwt__Key", "super_secret_test_key_1234567890123456");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "test_issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "test_audience");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");

        _client = factory.CreateClient();
        _factory = factory;
    }

    /*
    Test that signing up a new user creates a new user in the database and can then log on.
    */
    [Fact]
    public async Task SignUp_ValidUserCreatesUser()
    {
        // Create a new request
        SignUpRequestDTO request = new(
            email: "newuser@test.com",
            password: "Password123!",
            firstName: "John",
            lastName: "Doe",
            role: "Admin");

        // Send this request to the client -> notice here you are hitting the full endpoint
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/signup", request);
        response.EnsureSuccessStatusCode();
        // The response type needs to be deconstructed back into your LoginResponseDTO if you want to inspect it
        LoginResponseDTO? result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The scope from you request ended, so you need a new scope to get access to the DB
        using IServiceScope scope = _factory.Services.CreateScope();
        // Retrieve the db context from the scope 
        ClinicianDbContext db = scope.ServiceProvider.GetRequiredService<ClinicianDbContext>();

        // Check that the user can logon now and retrieves a valid token
        LoginRequestDTO loginRequest = new("newuser@test.com", "Password123!");
        HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        LoginResponseDTO? token = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

        // Check the db directly for the test you ran
        var user = db.Users.FirstOrDefault(u => u.Email == "newuser@test.com");

        Assert.NotNull(user);
        Assert.NotNull(token!.Token);
        Assert.Equal(token!.FirstName, user.FirstName);
    }

    /*
    Test that invalid credentials fails login
    */


}