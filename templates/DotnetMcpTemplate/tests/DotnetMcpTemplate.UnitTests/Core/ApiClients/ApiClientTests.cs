using System.Net;
using Microsoft.Extensions.Configuration;
using DotnetMcpTemplate.Core.ApiClients;
using DotnetMcpTemplate.Integrations.SampleApi;

namespace DotnetMcpTemplate.UnitTests.Core.ApiClients;

public sealed class ApiClientTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private static (SampleApiClient Client, StubHandler Handler) Create(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/v1/") };
        return (new SampleApiClient(http), handler);
    }

    [Fact]
    public async Task Get_deserializes_json_and_keeps_base_path()
    {
        var (client, handler) = Create(HttpStatusCode.OK, """{"id":1,"name":"Leanne","username":"Bret","email":"l@example.com"}""");

        var user = await client.GetUserAsync(1, CancellationToken.None);

        Assert.Equal("Leanne", user!.Name);
        Assert.Equal("https://api.example.com/v1/users/1", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Query_parameters_are_added()
    {
        var (client, handler) = Create(HttpStatusCode.OK, "[]");

        await client.GetPostsAsync(7, CancellationToken.None);

        Assert.Equal("https://api.example.com/v1/posts?userId=7", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Not_found_maps_to_null_in_the_client()
    {
        var (client, _) = Create(HttpStatusCode.NotFound, "{}");
        Assert.Null(await client.GetUserAsync(99, CancellationToken.None));
    }

    [Fact]
    public async Task Server_errors_become_safe_messages()
    {
        var (client, _) = Create(HttpStatusCode.InternalServerError, """{"stack":"secret internals"}""");

        var exception = await Assert.ThrowsAsync<ApiClientException>(() => client.GetUsersAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.DoesNotContain("secret", exception.SafeMessage);
    }

    [Fact]
    public void Settings_errors_name_the_variables()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CRM_BASE_URL"] = "not-a-url", ["CRM_AUTH"] = "api_key" })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => ApiClientRegistration.ReadSettings(configuration, "CRM"));

        Assert.Contains("CRM_BASE_URL", exception.Message);
    }

    [Fact]
    public void Default_headers_are_parsed()
    {
        var settings = new ApiClientSettings { Headers = "X-Tenant=acme; X-Env = test" };
        Assert.Equal("acme", settings.DefaultHeaders["X-Tenant"]);
        Assert.Equal("test", settings.DefaultHeaders["x-env"]);
    }
}
