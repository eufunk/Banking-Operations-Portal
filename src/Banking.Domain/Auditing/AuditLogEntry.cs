using Banking.Domain.Common;

namespace Banking.Domain.Auditing;

/// <summary>
/// Unveränderliches Protokoll einer Änderung an einer beliebigen Entität im System.
/// Bewusst ohne Verhalten/Statusübergänge (anders als die übrigen Aggregate) - ein
/// Audit-Eintrag wird einmal geschrieben und nie wieder verändert.
///
/// EntityId/EntityType sind bewusst generisch (Guid/string) statt stark typisiert:
/// Auditing ist ein cross-cutting Modul und darf keine Abhängigkeit auf die
/// konkreten ID-Typen der Fachmodule (CustomerId, AccountId, ...) haben.
/// </summary>
public sealed class AuditLogEntry : Entity<AuditLogEntryId>
{
    public const int MaxActionLength = 100;
    public const int MaxEntityTypeLength = 100;

    public Guid UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }

    // Für EF Core.
    private AuditLogEntry()
    {
        Action = string.Empty;
        EntityType = string.Empty;
    }

    private AuditLogEntry(
        AuditLogEntryId id,
        Guid userId,
        DateTime timestamp,
        string action,
        string entityType,
        Guid entityId,
        string? oldValue,
        string? newValue)
        : base(id)
    {
        UserId = userId;
        Timestamp = timestamp;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public static AuditLogEntry Create(Guid userId, string action, string entityType, Guid entityId, string? oldValue, string? newValue)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action darf nicht leer sein.", nameof(action));
        if (action.Length > MaxActionLength)
            throw new ArgumentException($"Action darf maximal {MaxActionLength} Zeichen lang sein.", nameof(action));
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("EntityType darf nicht leer sein.", nameof(entityType));
        if (entityType.Length > MaxEntityTypeLength)
            throw new ArgumentException($"EntityType darf maximal {MaxEntityTypeLength} Zeichen lang sein.", nameof(entityType));

        return new AuditLogEntry(
            AuditLogEntryId.New(),
            userId,
            DateTime.UtcNow,
            action.Trim(),
            entityType.Trim(),
            entityId,
            oldValue,
            newValue);
    }
}
