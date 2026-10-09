using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

namespace FleetCompany.FleetManagement.Modules.Operations.Infrastructure;

public static class OperationsModuleRegistration
{
    public static IServiceCollection AddOperationsModule<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IMissionRepository, MissionRepository<TContext>>();
        services.AddScoped<IMissionReadModel, MissionReadModel<TContext>>();
        services.AddScoped<IResourceReservations, ResourceReservations<TContext>>();
        services.AddScoped<MissionWorkflow>();
        return services;
    }
}
