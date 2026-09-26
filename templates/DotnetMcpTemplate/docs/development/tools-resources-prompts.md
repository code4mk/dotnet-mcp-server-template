# Tools, resources and prompts

| Component | Folder | Attribute on class | Attribute on method | What it is |
| --- | --- | --- | --- | --- |
| Tool | `Capabilities/Tools/` | `[McpServerToolType]` | `[McpServerTool(Name = "...")]` | An action the model calls |
| Resource | `Capabilities/Resources/` | `[McpServerResourceType]` | `[McpServerResource(UriTemplate = "...", Name = "...")]` | Data read by URI |
| Prompt | `Capabilities/Prompts/` | `[McpServerPromptType]` | `[McpServerPrompt(Name = "...")]` | A reusable prompt template |

Every class needs `[Authorize]` or `[AllowAnonymous]`; every method needs an explicit `Name` (architecture tests).
Classes are discovered automatically; there is no registration line.

## Adding a tool

```csharp
[McpServerToolType]
[Authorize]
public sealed class InvoiceTools(IInvoiceService invoices)
{
    [McpServerTool(Name = "get_invoice", ReadOnly = true, Destructive = false)]
    [Description("Gets an invoice by number.")]
    public Task<InvoiceDto> GetInvoice(
        [Required, RegularExpression("^INV-[0-9]{6}$"), Description("Invoice number, e.g. INV-000123.")] string number,
        AppUser user,
        CancellationToken cancellationToken) =>
        invoices.GetAsync(number, user, cancellationToken);
}
```

- Put the logic in a service (`Services/`, registered in `ServicesSetup.cs`), inputs in `Models/Requests`, outputs in
  `Models/Responses`. External data comes from a client in `Integrations/`, called by the service, never the tool.
- Inject `AppUser` (or `ClaimsPrincipal`) for the caller; it is hidden from the schema.
- Set annotations honestly: `ReadOnly`, `Destructive`, `Idempotent`, `OpenWorld` help clients decide on confirmations.
- Throw `AppException.NotFound/Conflict/Rule(...)` for expected errors; the model receives the message.
- Use `[Authorize(Policy = Policies.X)]` on a method for extra requirements (scopes, roles).
- Return a record. Set `UseStructuredContent = true` when clients or an MCP App read the result as data: the tool
  then returns `structuredContent` and publishes an output schema. Without it the result is JSON text.

## Resources

Template values arrive as strings: validate them (`ResourceArguments.PositiveInt`). Return `TextResourceContents`
(`JsonResource.Create(uri, dto)`) or a string for text resources.

## Prompts

Return `ChatMessage` or `IEnumerable<ChatMessage>`. Prompts can call services to embed data. Arguments are validated
like tool arguments.
