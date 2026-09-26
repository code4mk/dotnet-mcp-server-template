using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using DotnetMcpTemplate.Models.Requests;
using DotnetMcpTemplate.Core.Validation;

namespace DotnetMcpTemplate.UnitTests.Core.Validation;

public sealed class RequestValidatorTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Stand-ins for tool methods.
    private static void Search([Required, StringLength(100, MinimumLength = 2)] string query, [Range(1, 20)] int limit = 5, CancellationToken cancellationToken = default) { }

    private static void Create(CreateProjectRequest request) { }

    private static IReadOnlyList<ValidationError> Validate(string method, string json) =>
        RequestValidator.Validate(
            typeof(RequestValidatorTests).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!,
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json),
            type => type == typeof(CancellationToken),
            Json);

    [Fact]
    public void Valid_primitive_arguments_pass()
    {
        Assert.Empty(Validate(nameof(Search), """{"query":"website","limit":3}"""));
    }

    [Fact]
    public void Missing_required_argument_is_reported()
    {
        var error = Assert.Single(Validate(nameof(Search), "{}"));
        Assert.Equal("query", error.Path);
    }

    [Fact]
    public void Parameter_attributes_are_checked()
    {
        var errors = Validate(nameof(Search), """{"query":"a","limit":50}""");
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Path == "limit");
    }

    [Fact]
    public void Wrong_type_is_reported_clearly()
    {
        var error = Assert.Single(Validate(nameof(Search), """{"query":"website","limit":"many"}"""));
        Assert.Contains("a number", error.Message);
    }

    [Fact]
    public void Request_object_properties_use_json_paths()
    {
        var errors = Validate(nameof(Create), """{"request":{"name":"ab","priority":"urgent","durationDays":10,"ownerUserId":1}}""");
        Assert.Contains(errors, e => e.Path == "request.name");
        Assert.Contains(errors, e => e.Path == "request.priority");
    }

    [Fact]
    public void Cross_field_rules_run_when_fields_are_valid()
    {
        var error = Assert.Single(Validate(nameof(Create), """{"request":{"name":"Big launch","priority":"high","durationDays":120,"ownerUserId":1}}"""));
        Assert.Equal("request.durationDays", error.Path);
    }

    [Fact]
    public void Format_lists_every_error()
    {
        var text = RequestValidator.Format("search", [new("query", "query is required."), new("limit", "too big")]);
        Assert.StartsWith("Invalid arguments for search:", text);
        Assert.Contains("- limit: too big", text);
    }
}
