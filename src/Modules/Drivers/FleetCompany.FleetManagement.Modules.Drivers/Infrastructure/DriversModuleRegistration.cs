using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Ports;
using FleetCompany.FleetManagement.Modules.Drivers.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Drivers.Contracts;

namespace FleetCompany.FleetManagement.Modules.Drivers.Infrastructure;

public static class DriversModuleRegistration
{
    public static IServiceCollection AddDriversModule<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IDriverRepository, DriverRepository<TContext>>();
        services.AddScoped<IDriverLookup, DriverLookup<TContext>>();
        return services;
    }
}
