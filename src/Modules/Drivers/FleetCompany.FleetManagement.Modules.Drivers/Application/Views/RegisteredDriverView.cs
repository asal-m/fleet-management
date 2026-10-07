using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
namespace FleetCompany.FleetManagement.Modules.Drivers.Application.Views;

public sealed record RegisteredDriverView(Guid Id, string FirstName, string LastName, DriverStatus Status, IReadOnlyList<string> Qualifications);
