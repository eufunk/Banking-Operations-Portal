using System.Text.Json;
using System.Text.Json.Serialization;

namespace Banking.Web.Services;

internal static class ApiJsonOptions
{
    /// <summary>Muss zur Serialisierung der Api spiegelbildlich passen (JsonStringEnumConverter, camelCase).</summary>
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
