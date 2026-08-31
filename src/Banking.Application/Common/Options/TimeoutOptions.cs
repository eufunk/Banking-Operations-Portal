namespace Banking.Application.Common.Options;

/// <summary>"Configuration" - Betriebsparameter, kein Secret.</summary>
public sealed class TimeoutOptions
{
    public const string SectionName = "Timeouts";

    public TimeSpan DatabaseCommandTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
