using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Banking.Api.Serialization;

/// <summary>
/// System.Text.Json kennt unsere Strongly-Typed-IDs (CustomerId, AccountId, ...) nicht -
/// ohne diesen Converter würde die Serialisierung mit "Unable to serialize..." scheitern.
/// Statt eines handgeschriebenen Converters pro ID-Typ: eine generische Factory, die per
/// Reflection jeden "readonly record struct XxxId(Guid Value)" aus dem Domain-Modell
/// erkennt und auf der Wire einheitlich als reiner GUID-String darstellt - egal ob in
/// einem Route-Segment, Query-Parameter oder JSON-Body.
/// </summary>
public sealed class StronglyTypedIdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsValueType
            || typeToConvert.Namespace is null
            || !typeToConvert.Namespace.StartsWith("Banking.Domain", StringComparison.Ordinal))
        {
            return false;
        }

        var valueProperty = typeToConvert.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);

        return valueProperty is not null
            && valueProperty.PropertyType == typeof(Guid)
            && typeToConvert.GetConstructor([typeof(Guid)]) is not null;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StronglyTypedIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class StronglyTypedIdJsonConverter<T> : JsonConverter<T>
    {
        private static readonly ConstructorInfo Constructor = typeof(T).GetConstructor([typeof(Guid)])!;
        private static readonly PropertyInfo ValueProperty = typeof(T).GetProperty("Value")!;

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var guid = reader.GetGuid();
            return (T)Constructor.Invoke([guid]);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var guid = (Guid)ValueProperty.GetValue(value)!;
            writer.WriteStringValue(guid);
        }
    }
}
