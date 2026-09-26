using System.ComponentModel;
using DotnetMcpTemplate.Core.Auth;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Tools;

[McpServerToolType]
[Authorize]
public sealed class MeTools
{
    [McpServerTool(Name = "whoami", Title = "Who am I", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Returns the signed-in user: id, name, email, roles, groups, scopes and all identity claims.")]
    public static AppUser WhoAmI(AppUser user) => user;
}
