# Validation

Three layers:

1. **Attributes on inputs** describe the JSON schema the model sees: `[Required]`, `[Range]`, `[StringLength]`,
   `[RegularExpression]`, `[EmailAddress]`, `[Url]`, `[AllowedValues]`, ... Put them on simple parameters
   (`[Range(1, 20)] int limit`) or on request records with `[property: ...]`.
2. **`ValidationFilter`** (`Core/Validation/`) checks every tool call and prompt before the handler runs: parameter attributes, request
   object properties, nested objects and collections, and `IValidatableObject` for cross-field rules. Tools answer
   with `isError: true` and a list the model can act on:

   ```text
   Invalid arguments for create_project:
   - request.name: The field name must be a string with a minimum length of 3 and a maximum length of 100.
   - request.durationDays: High-priority projects must be 90 days or less.
   ```

   Prompts get an MCP invalid-params error. Resource URI values are validated in the resource method.
3. **Services** check rules that need data (duplicates, permissions) and throw `AppException`.

Parameters resolved from DI (services, `AppUser`, `CancellationToken`) are never validated as input.
