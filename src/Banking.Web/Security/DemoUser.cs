namespace Banking.Web.Security;

public sealed record DemoUser(string Username, string PasswordHash, string DisplayName, string Role);
