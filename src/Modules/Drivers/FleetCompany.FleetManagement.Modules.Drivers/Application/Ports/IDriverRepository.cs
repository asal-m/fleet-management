using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;

namespace FleetCompany.FleetManagement.Modules.Drivers.Application.Ports;

public interface IDriverRepository
{
    void Add(Driver driver);
}
