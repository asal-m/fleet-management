using MPCore.Domain.Model;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers.Rules;
namespace FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;

public sealed class Driver : AggregateRoot<Guid>
{
    private readonly List<QualificationCode> _qualifications = [];
    private Driver() { FirstName = null!; LastName = null!; }
    public DriverName FirstName { get; private set; }
    public DriverName LastName { get; private set; }
    public DriverStatus Status { get; private set; }
    public IReadOnlyCollection<QualificationCode> Qualifications => _qualifications.AsReadOnly();
    // Mission reservations belong to Operations; this property checks only Drivers state.
    public bool IsActive => Status == DriverStatus.Active;
    private Driver(Guid id, DriverName firstName, DriverName lastName,
        DriverStatus status, IEnumerable<QualificationCode> qualifications) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);
        ArgumentNullException.ThrowIfNull(qualifications);
        CheckRule(new DriverStatusMustBeValidRule(status));
        var distinct = qualifications.Distinct().ToArray();
        CheckRule(new DriverQualificationsMustBeValidRule(distinct));
        FirstName = firstName;
        LastName = lastName;
        Status = status;
        _qualifications.AddRange(distinct);
    }
    public static Driver Register(Guid id, DriverName firstName, DriverName lastName,
        DriverStatus status, IEnumerable<QualificationCode> qualifications)
        => new(id, firstName, lastName, status, qualifications);
    public bool IsQualifiedFor(QualificationCode vehicleType)
    {
        ArgumentNullException.ThrowIfNull(vehicleType);
        return _qualifications.Contains(vehicleType);
    }
}
