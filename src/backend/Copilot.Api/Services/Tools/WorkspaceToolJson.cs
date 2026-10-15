using System.Text.Json;
using System.Text.Json.Serialization;

namespace Copilot.Api.Services.Tools;

internal static class WorkspaceToolJson
{
    private static readonly JsonSerializerOptions SerializerOptions =
        CreateSerializerOptions();

    public static JsonSerializerOptions Options => SerializerOptions;

    public static JsonElement ParseObject(
        string argumentsJson,
        out JsonDocument document)
    {
        document = JsonDocument.Parse(argumentsJson);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            throw new ArgumentException(
                "Tool arguments must be a JSON object.",
                nameof(argumentsJson));
        }

        return root;
    }

    public static string GetRequiredString(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException(
                $"The '{name}' argument is required.");
        }

        var result = value.GetString();

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new ArgumentException(
                $"The '{name}' argument cannot be empty.");
        }

        return result;
    }

    public static string? GetOptionalString(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException(
                $"The '{name}' argument must be a string.");
        }

        return value.GetString();
    }

    public static Guid GetRequiredGuid(
        JsonElement root,
        string name)
    {
        var value = GetRequiredString(root, name);

        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
        {
            throw new ArgumentException(
                $"The '{name}' argument must be a non-empty GUID.");
        }

        return id;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
