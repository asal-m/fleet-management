using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Fleet.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;

namespace FleetCompany.FleetManagement.Modules.Fleet.Infrastructure;

public static class FleetModuleRegistration
{
    public static IServiceCollection AddFleetModule<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IVehicleRepository, VehicleRepository<TContext>>();
        services.AddScoped<IVehicleReadModel, VehicleReadModel<TContext>>();
        services.AddScoped<IFleetLookup, FleetLookup<TContext>>();
        services.AddScoped<VehicleWorkflow>();
        return services;
    }
}
