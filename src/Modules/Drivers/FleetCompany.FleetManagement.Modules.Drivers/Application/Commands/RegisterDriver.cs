using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Ports;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Views;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
namespace FleetCompany.FleetManagement.Modules.Drivers.Application.Commands;

public sealed record RegisterDriver(string? FirstName, string? LastName, DriverStatus? Status,
    IReadOnlyList<string?>? Qualifications) : ICommand<Result<RegisteredDriverView>>
{
    public override string ToString() => nameof(RegisterDriver);
}
public sealed class RegisterDriverHandler(IDriverRepository repository, IAvailabilityCoordinator coordinator)
{
    public async Task<Result<RegisteredDriverView>> Handle(RegisterDriver command, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = unitOfWork; // The host middleware owns commit.
        var first = DriverName.Create(command.FirstName);
        await coordinator.AcquireAsync(cancellationToken);
        var last = DriverName.Create(command.LastName);
        var status = command.Status ?? throw new ArgumentNullException(nameof(command.Status));
        var codes = (command.Qualifications ?? []).Select(QualificationCode.Create).ToArray();
        var driver = Driver.Register(Guid.CreateVersion7(), first, last, status, codes);
        repository.Add(driver);
        await coordinator.ChangedAsync(cancellationToken);
        return Result<RegisteredDriverView>.Success(new(driver.Id, first.Value, last.Value,
            driver.Status, driver.Qualifications.Select(code => code.Value).ToArray()));
    }
}
