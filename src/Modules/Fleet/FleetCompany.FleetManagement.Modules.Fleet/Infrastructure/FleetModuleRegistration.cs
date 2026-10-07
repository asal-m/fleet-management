using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;

namespace FleetCompany.FleetManagement.Modules.Fleet.Infrastructure;

public static class FleetModuleRegistration
{
    public static IServiceCollection AddFleetModule<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IVehicleRepository, VehicleRepository<TContext>>();
        services.AddScoped<IVehicleReadModel, VehicleReadModel<TContext>>();
        services.AddScoped<FleetCompany.FleetManagement.Modules.Fleet.Contracts.IFleetLookup, FleetLookup<TContext>>();
        services.AddScoped<FleetCompany.FleetManagement.Modules.Fleet.Application.Commands.VehicleWorkflow>();
        return services;
    }
}
