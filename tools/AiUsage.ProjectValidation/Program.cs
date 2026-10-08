using System.Text.Json;
using AiUsage.ProjectValidation;

if (args.Length is not (3 or 4) || args[0] != "--root" || args[2] != "--json" || args.Length == 4 && args[3] != "--final")
{
    Console.Error.WriteLine("Usage: AiUsage.ProjectValidation --root <repository> --json [--final]");
    return 2;
}
try
{
    var diagnostics = ProjectValidator.Validate(args[1], final: args.Length == 4);
    Console.WriteLine(JsonSerializer.Serialize(new { valid = diagnostics.Count == 0, diagnostics }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    return diagnostics.Count == 0 ? 0 : 1;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
{
    Console.Error.WriteLine($"Validation could not read the requested repository ({error.GetType().Name}).");
    return 2;
}
