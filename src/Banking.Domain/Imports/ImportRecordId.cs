namespace Banking.Domain.Imports;

public readonly record struct ImportRecordId(Guid Value)
{
    public static ImportRecordId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
