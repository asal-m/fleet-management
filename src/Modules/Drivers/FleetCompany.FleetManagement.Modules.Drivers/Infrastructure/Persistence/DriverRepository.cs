using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Ports;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
namespace FleetCompany.FleetManagement.Modules.Drivers.Infrastructure.Persistence;

public sealed class DriverRepository<TContext>(TContext database) : IDriverRepository where TContext : DbContext
{
    public void Add(Driver driver) => database.Set<Driver>().Add(driver);
}
