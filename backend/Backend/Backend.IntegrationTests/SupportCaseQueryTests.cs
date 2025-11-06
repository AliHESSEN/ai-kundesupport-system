using Backend.IntegrationTests.Infrastructure;
using Backend.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

public class SupportCaseQueryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SupportCaseQueryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_cases_as_User_returns_only_own_cases()
    {
        // lag to brukere
        var (clientA, userA) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");
        var (clientB, userB) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");

        // A lager sak
        var respA1 = await clientA.PostAsJsonAsync("/cases", new SupportCase { Title = "A1", Description = "desc", Status = "Open" });
        respA1.StatusCode.Should().Be(HttpStatusCode.Created);

        // B lager sak
        var respB1 = await clientB.PostAsJsonAsync("/cases", new SupportCase { Title = "B1", Description = "desc", Status = "Open" });
        respB1.StatusCode.Should().Be(HttpStatusCode.Created);

        // A henter sine saker (skal bare se sin egen)
        var getA = await clientA.GetAsync("/cases");
        getA.StatusCode.Should().Be(HttpStatusCode.OK);
        var aCases = await getA.Content.ReadFromJsonAsync<List<SupportCase>>();
        aCases!.Should().OnlyContain(c => c.CreatedById == userA);
    }

    [Fact]
    public async Task Get_cases_as_SupportStaff_returns_all_cases()
    {
        // opprett to saker som users
        var (userClient1, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");
        var (userClient2, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");

        var createResp1 = await userClient1.PostAsJsonAsync("/cases", new SupportCase { Title = "S1", Description = "desc", Status = "Open" });
        createResp1.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResp2 = await userClient2.PostAsJsonAsync("/cases", new SupportCase { Title = "S2", Description = "desc", Status = "Open" });
        createResp2.StatusCode.Should().Be(HttpStatusCode.Created);

        // SupportStaff ser alt
        var (staffClient, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "SupportStaff");
        var resp = await staffClient.GetAsync("/cases");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var all = await resp.Content.ReadFromJsonAsync<List<SupportCase>>();
        all!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Get_cases_supports_status_filter()
    {
        var (userClient, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");

        (await userClient.PostAsJsonAsync("/cases", new SupportCase { Title = "S1", Description = "desc", Status = "Open" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        (await userClient.PostAsJsonAsync("/cases", new SupportCase { Title = "S2", Description = "desc", Status = "Closed" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var resp = await userClient.GetAsync("/cases?status=Closed");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var closed = await resp.Content.ReadFromJsonAsync<List<SupportCase>>();
        closed!.Should().OnlyContain(c => c.Status == "Closed");
    }

    [Fact]
    public async Task Get_cases_supports_search_in_title_and_description()
    {
        var (userClient, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "User");

        (await userClient.PostAsJsonAsync("/cases", new SupportCase { Title = "Printer", Description = "Paper jam", Status = "Open" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        (await userClient.PostAsJsonAsync("/cases", new SupportCase { Title = "Network", Description = "Wifi unstable", Status = "Open" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var resp = await userClient.GetAsync("/cases?search=printer");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await resp.Content.ReadFromJsonAsync<List<SupportCase>>();
        list!.Should().OnlyContain(c => c.Title.Contains("Printer", StringComparison.OrdinalIgnoreCase)
                                     || c.Description.Contains("Printer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Get_cases_with_unknown_role_returns_403()
    {
        // lager en bruker i "Guest" rolle som ikke har tilgang
        var (client, _) = await TestAuthHelpers.CreateClientWithUserRoleAsync(_factory, "Guest");
        var resp = await client.GetAsync("/cases");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
