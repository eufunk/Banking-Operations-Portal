using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Banking.Application.Common;

/// <summary>
/// ActivitySource (Tracing) und Meter (Metriken) sind reine .NET-Bordmittel
/// (System.Diagnostics), kein Azure-/OpenTelemetry-Paket nötig - deshalb dürfen sie hier
/// in der Application-Schicht leben, ohne die ASP.NET-Core-Kopplungsregel zu verletzen.
/// Banking.Api registriert diese Quellen beim OpenTelemetry-SDK (siehe Program.cs); ohne
/// diese Registrierung sind ActivitySource/Meter reine No-Ops - der Code hier funktioniert
/// also unabhängig davon, ob Observability überhaupt aktiv beobachtet wird.
/// </summary>
public static class Telemetry
{
    public const string ServiceName = "Banking.Application";

    public static readonly ActivitySource ActivitySource = new(ServiceName);

    private static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> PaymentSuccessCounter = Meter.CreateCounter<long>(
        "payment.success", unit: "{payment}", description: "Anzahl erfolgreich angelegter Zahlungsaufträge");

    public static readonly Counter<long> PaymentFailedCounter = Meter.CreateCounter<long>(
        "payment.failed", unit: "{payment}", description: "Anzahl fehlgeschlagener Zahlungsauftrag-Anlagen, nach Grund (Tag 'reason')");

    public static readonly Histogram<double> PaymentProcessingDuration = Meter.CreateHistogram<double>(
        "payment.processing.duration", unit: "ms", description: "Dauer der Zahlungsauftrag-Verarbeitung von Validierung bis Speichern");

    public static readonly Histogram<double> TransactionSearchDuration = Meter.CreateHistogram<double>(
        "transaction.search.duration", unit: "ms", description: "Dauer der Transaktionssuche");
}
