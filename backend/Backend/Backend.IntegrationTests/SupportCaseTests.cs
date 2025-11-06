using Backend.IntegrationTests.Infrastructure;
using Backend.Models;
using Backend.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

public class SupportCaseTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SupportCaseTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_case_as_User_returns_201_and_persists_in_db()
    {
        // lager klient med User-rolle
        var (client, userId) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");

        var resp = await client.PostAsJsonAsync("/cases", new SupportCase
        {
            Title = "Printer broken",
            Description = "Paper jam",
            Status = "Open"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await resp.Content.ReadFromJsonAsync<SupportCase>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("Printer broken");
        created.CreatedById.Should().Be(userId);
    }

    [Fact]
    public async Task Patch_case_as_SupportStaff_returns_200_and_updates_status()
    {
        // først lager vi en sak som en vanlig bruker
        var (userClient, userId) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");
        var createResp = await userClient.PostAsJsonAsync("/cases", new SupportCase
        {
            Title = "VPN down",
            Description = "Cannot connect",
            Status = "Open"
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<SupportCase>();
        created.Should().NotBeNull();

        // patch gjøres av SupportStaff
        var (staffClient, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "SupportStaff");
        var patchResp = await staffClient.PatchAsJsonAsync($"/cases/{created!.Id}", new UpdateCaseStatusRequest
        {
            Status = "Closed"
        });

        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await patchResp.Content.ReadFromJsonAsync<SupportCase>();
        updated!.Status.Should().Be("Closed");
        updated.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Patch_case_as_User_returns_403()
    {
        // opprett sak
        var (userAClient, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");
        var createResp = await userAClient.PostAsJsonAsync("/cases", new SupportCase
        {
            Title = "Email issue",
            Description = "Cannot send",
            Status = "Open"
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<SupportCase>();

        // samme bruker prøver å patch'e (skal ikke være lov)
        var patchResp = await userAClient.PatchAsJsonAsync($"/cases/{created!.Id}", new UpdateCaseStatusRequest
        {
            Status = "Closed"
        });

        patchResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
