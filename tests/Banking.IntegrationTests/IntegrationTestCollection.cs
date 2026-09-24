namespace Banking.IntegrationTests;

/// <summary>
/// Alle Integration-Test-Klassen teilen sich eine Factory-Instanz (und damit eine
/// Datenbank) - das Anlegen+Migrieren einer frischen LocalDB-Datenbank pro Testklasse
/// wäre unnötig langsam. Tests dürfen sich deshalb nicht auf einen bestimmten
/// Gesamtzustand der Datenbank verlassen (siehe IntegrationTestWebApplicationFactory).
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebApplicationFactory>
{
    public const string Name = "Integration Tests";
}
