using Banking.Domain.Common;
using Banking.Domain.Common.Exceptions;

namespace Banking.Domain.Customers;

public sealed class Customer : Entity<CustomerId>
{
    public CustomerNumber CustomerNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public CustomerStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Für EF Core; private Setter werden per Reflection bei der Materialisierung befüllt.
    private Customer()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    private Customer(
        CustomerId id,
        CustomerNumber customerNumber,
        string firstName,
        string lastName,
        Email email,
        CustomerStatus status,
        DateTime createdAt)
        : base(id)
    {
        CustomerNumber = customerNumber;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Customer Register(CustomerNumber customerNumber, string firstName, string lastName, Email email)
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));

        return new Customer(
            CustomerId.New(),
            customerNumber,
            firstName.Trim(),
            lastName.Trim(),
            email,
            CustomerStatus.Active,
            DateTime.UtcNow);
    }

    public void UpdateContactInfo(string firstName, string lastName, Email email)
    {
        ValidateName(firstName, nameof(firstName));
        ValidateName(lastName, nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email;
    }

    public void Activate() => TransitionTo(CustomerStatus.Active, CustomerStatus.Inactive, CustomerStatus.Blocked);

    public void Deactivate() => TransitionTo(CustomerStatus.Inactive, CustomerStatus.Active);

    public void Block() => TransitionTo(CustomerStatus.Blocked, CustomerStatus.Active, CustomerStatus.Inactive);

    public void Close() => TransitionTo(CustomerStatus.Closed, CustomerStatus.Active, CustomerStatus.Inactive, CustomerStatus.Blocked);

    private void TransitionTo(CustomerStatus target, params CustomerStatus[] allowedFrom)
    {
        if (Status == CustomerStatus.Closed || !allowedFrom.Contains(Status))
            throw new InvalidStatusTransitionException(nameof(Customer), Status.ToString(), target.ToString());

        Status = target;
    }

    private static void ValidateName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name darf nicht leer sein.", paramName);
        if (name.Length > 100)
            throw new ArgumentException("Name darf maximal 100 Zeichen lang sein.", paramName);
    }
}
