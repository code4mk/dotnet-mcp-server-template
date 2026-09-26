using DotnetMcpTemplate.Core.Auth;
using DotnetMcpTemplate.Core.Common.Errors;
using DotnetMcpTemplate.Core.Common.Middleware;
using DotnetMcpTemplate.Core.Common.Settings;
using DotnetMcpTemplate.Core.Mcp;
using DotnetMcpTemplate.Core.ServerInfo;
using DotnetMcpTemplate.Integrations;
using DotnetMcpTemplate.Services;

EnvFile.Load();                                               // .env → environment, APP_ENV → ASP.NET environment

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseAppPort(builder.Configuration);            // listen on APP_PORT

builder.Services.AddAllEnvSettings(builder.Configuration);    // typed, validated settings
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddAppAuth(builder.Configuration);           // auth provider, policies, AppUser
builder.Services.AddIntegrations(builder.Configuration);       // external APIs / data sources
builder.Services.AddAppServices();                            // business services
builder.Services.AddAppMcpServer();                           // tools, resources, prompts, filters, MCP Apps

var app = builder.Build();
app.ValidateSettings();                                        // report every settings error at once
app.LogPublicUrl();                                            // "Server running at http://localhost:5080"

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapServerInfo();                                           // GET /
app.MapHealthChecks("/health");
app.MapAppAuth();                                              // provider endpoints (OAuth proxy)
app.MapAppMcp();                                               // MCP endpoint (MCP_PATH)

await app.RunAsync();

/// <summary>Visible to WebApplicationFactory in integration tests.</summary>
public partial class Program;
