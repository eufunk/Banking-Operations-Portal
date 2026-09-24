using System.Text.Json;
using System.Text.Json.Serialization;
using Banking.Api.Serialization;

namespace Banking.IntegrationTests;

/// <summary>
/// Muss spiegelbildlich zu den JSON-Optionen der Api passen (Banking.Api/Program.cs:
/// StronglyTypedIdJsonConverterFactory + JsonStringEnumConverter, camelCase) - sonst
/// scheitert die Deserialisierung von Ids (CustomerId/AccountId/...) und Enums
/// (PaymentStatus/CustomerStatus/...) in den Testclients, obwohl die Api korrekt antwortet.
/// Exakt dasselbe Muster wie Banking.Web/Services/ApiJsonOptions.cs.
/// </summary>
internal static class IntegrationTestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new StronglyTypedIdJsonConverterFactory() },
    };
}
