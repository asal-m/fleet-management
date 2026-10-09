using FleetCompany.FleetManagement.Api.Hosting;
using FleetCompany.FleetManagement.Infrastructure;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using MPCore.Hosting;
using MPCore.Localization;
using MPCore.Messaging.Wolverine;
using MPCore.Observability;
using MPCore.Observability.Prometheus;
using MPCore.Security.AspNetCore;
using MPCore.Validation.FluentValidation;
using FleetCompany.FleetManagement.Api.Grpc.Services;
using MPCore.Transport.Grpc;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FleetCompany.FleetManagement.Api.Rest.Endpoints;
using MPCore.Transport.Http;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Text.Json.Serialization;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using FleetCompany.FleetManagement.Api.Rest;
using FleetCompany.FleetManagement.Modules.Fleet.Infrastructure;
using FleetCompany.FleetManagement.Modules.Drivers.Infrastructure;
using FleetCompany.FleetManagement.Modules.Operations.Infrastructure;
using System.Diagnostics;
using FleetCompany.FleetManagement.Modules.Fleet.Resources;
using FleetCompany.FleetManagement.Modules.Drivers.Resources;
using FleetCompany.FleetManagement.Modules.Operations.Resources;
using FleetCompany.FleetManagement.Modules.Administration.Resources;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Named business states in responses; existing numeric request values remain supported.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<VehicleBaseStatus>());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<VehicleOperationalStatus>());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<DriverStatus>());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<MissionStatus>());
});
var localConfigurationFile = builder.Configuration["LocalEnvironment:ConfigurationFile"];
if (!string.IsNullOrWhiteSpace(localConfigurationFile))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Compose configuration is Development-only.");
    builder.Configuration.AddJsonFile(localConfigurationFile, optional: false, reloadOnChange: false);
    // Deployment overrides retain precedence over the generated local file.
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddCommandLine(args);
}

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource("FleetCompany.FleetManagement.Business", "Npgsql"));
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter("FleetCompany.FleetManagement.Availability"));
builder.Logging.Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId);
// Per-endpoint Kestrel "Protocols" values in appsettings.json are authoritative; no protocol is
// forced globally.
// A single cleartext Http1AndHttp2 endpoint cannot serve prior-knowledge h2c HTTP/2, so the
// endpoint that carries binary RPC declares Http2 exclusively.
const TransportMode HostTransport = TransportMode.Both;
TransportEndpointGuard.Validate(builder.Configuration, HostTransport);
builder.Services.AddMPCoreFoundation(new MPCoreObservabilityOptions
{
    ServiceName = "FleetCompany.FleetManagement",
    ServiceNamespace = "FleetCompany",
    ServiceVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
    EnableOtlpExporter = builder.Configuration.GetValue("Observability:EnableOtlpExporter", false), // Each signal has its own destination, sampling and redaction settings; see docs/architecture.md.
    Signals = builder.Configuration.GetSection("Observability").Get<MPCoreObservabilitySignals>()
});
// Prometheus pull is a REST-listener surface. It carries no anonymous metadata: the scraper must
// present a bearer token, or the deployment must confine the listener to the scraper's network.
var metricsScrapeEnabled = builder.Configuration.GetValue("Observability:Metrics:Prometheus:Enabled", false);
if (metricsScrapeEnabled)
{
    builder.Services.AddMPCorePrometheusScrape();
}

// One registration per bounded-context module, each from its own project:
//     builder.Services.AddBillingModule();
// See src/Modules/README.md.
// Failures reach the caller as message keys, rendered here in the caller's language: MP Core's own
// messages ship in English and Persian, and each module adds its resource file. See
// docs/architecture.md, "Business rules, validation and messages".
builder.Services.AddMPCoreMessageCatalog(catalog =>
{
    catalog.AddResources<FleetMessages>();
    catalog.AddResources<DriversMessages>();
    catalog.AddResources<OperationsMessages>();
    catalog.AddResources<AdministrationMessages>();
});
// Input validators (FluentValidation) of every handler assembly. They run before the handler.
foreach (var assembly in HandlerAssemblies.All)
{
    builder.Services.AddMPCoreValidators(assembly);
}

// Bearer-only OIDC resource server. Login, signup, OTP, password and identity-provider
// administration are Product surfaces and are never implemented here.
builder.Services.AddMPCoreBearerAuthentication(options =>
{
    options.Authority = builder.Configuration["Security:Authority"] ?? throw new InvalidOperationException("Security:Authority is required.");
    options.RequireHttpsMetadata = builder.Configuration.GetValue("Security:RequireHttpsMetadata", true);
    foreach (var audience in builder.Configuration.GetSection("Security:Audiences").Get<string[]>() ?? [])
    {
        options.ValidAudiences.Add(audience);
    }
});
// Claim mapping follows the identity provider. Keycloak-shaped by default (realm and client roles,
// preferred_username, azp, service-account-* convention); GenericOidc reads a flat roles claim.
builder.Services.AddMPCoreCurrentActor(mapping =>
{
    if (string.Equals(builder.Configuration["Security:ClaimMapping:Preset"], "GenericOidc", StringComparison.OrdinalIgnoreCase))
    {
        mapping.UseGenericOidc();
    }
    else
    {
        mapping.UseKeycloakDefaults();
    }
});
// The tenant, when the token names one. Business code reads ITenantContext; audit records it.
builder.Services.AddMPCoreTenancyFromClaim(builder.Configuration["Security:TenantClaim"] ?? "tenant_id");
builder.Services.AddForwardedIdentityHeaderGuard();
// X-Forwarded-* is honoured only from the proxies listed here (APISIX or another gateway). Empty
// means the host reasons from the real connection and ignores the headers entirely.
builder.Services.AddMPCoreGatewayForwarding(options =>
{
    foreach (var proxy in builder.Configuration.GetSection("Gateway:TrustedProxies").Get<string[]>() ?? [])
    {
        options.TrustedProxies.Add(proxy);
    }
});
// MP Core does not map the health probes, so it cannot enforce anonymous access to them. This host
// maps them and therefore owns the decision, applied with AllowAnonymous() where they are mapped.
var allowAnonymousHealthEndpoints = builder.Configuration.GetValue("Security:AllowAnonymousHealthEndpoints", true);
builder.Services.AddMPCoreAuthorization();
// Description surfaces are opt-in and default to Development only: an OpenAPI document and gRPC
// reflection describe the entire API to whoever can reach them.
var enableOpenApi = DeveloperEndpoints.IsDescriptionSurfaceEnabled(builder.Configuration, builder.Environment, "Transport:EnableOpenApi");
var enableGrpcReflection = DeveloperEndpoints.IsDescriptionSurfaceEnabled(builder.Configuration, builder.Environment, "Transport:EnableGrpcReflection");
// A description surface is only reachable without a token in Development. Enabling one explicitly in
// another environment keeps it behind the authenticated fallback: the flag says "expose it", not
// "expose it to anyone".
var anonymousDescriptionSurface = builder.Environment.IsDevelopment();
// What "alive" and "ready" mean is the same on every transport: Hosting/HostHealthChecks.cs.
builder.Services.AddHostHealthChecks();
builder.Services.AddGrpc().AddMPCoreFailureHandling();
// The empty service name is the whole host; "live" asks the process only.
builder.Services.AddGrpcHealthChecks(options => options.Services.Map(HostHealthChecks.Live, static check => check.Tags.Contains(HostHealthChecks.Live)));
if (enableGrpcReflection)
{
    builder.Services.AddGrpcReflection();
}

builder.Services.AddMPCoreHttpFailureHandling();
builder.Services.AddMPCoreProblemDetailsSecurityResponses();
if (enableOpenApi)
{
    builder.Services.AddOpenApi();
}

var databaseConnection = builder.Configuration.GetConnectionString("PostgreSql") ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");
var cacheConnection = builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("ConnectionStrings:Redis is required for the selected cache.");
builder.Services.AddInfrastructure(databaseConnection, cacheConnection);
builder.Services.AddFleetModule<AppDbContext>();
builder.Services.AddDriversModule<AppDbContext>();
builder.Services.AddOperationsModule<AppDbContext>();
builder.Services.AddHttpExceptionMapper<VehicleExceptionMapper>();
builder.Services.AddSingleton<IGrpcExceptionMapper, VehicleExceptionMapper>();
// AppDbContext is named here as the transaction owner, so a handler can depend on IUnitOfWork and
// still run inside the Entity Framework transaction whose commit releases its outgoing messages.
builder.Host.UseMPCoreWolverine<AppDbContext>(new WolverineFoundationOptions
{
    ServiceName = "FleetCompany.FleetManagement",
    PersistenceConnectionString = databaseConnection,
    PersistenceSchemaName = "wolverine", // This project's own handlers are discovered from here, in addition to HandlerAssemblies.All.
    ApplicationAssembly = typeof(Program).Assembly
}, options =>
   {
       // Handlers are discovered only in the assemblies this host names. Nothing is scanned
       // implicitly and no catch-all policy exists; see Hosting/HandlerAssemblies.cs.
       options.DiscoverHandlersIn(HandlerAssemblies.All);
       // A message whose validators fail never reaches its handler; the caller receives MP Core's
       // validation failure with one violation per field.
       options.UseMPCoreFluentValidation();
   });
var app = builder.Build();
// ADR-007 pipeline order. UseAuthentication always precedes UseAuthorization, and both follow
// UseRouting so the authenticated fallback policy sees resolved endpoint metadata.
// Forwarded headers come first, so everything after it sees the scheme and client the gateway saw.
app.UseMPCoreGatewayForwarding();
app.UseMPCoreProblemDetails();
app.UseMPCoreRequestContext();
var businessTrace = new ActivitySource("FleetCompany.FleetManagement.Business");
app.Use(async (context, next) =>
{
    using var span = businessTrace.StartActivity("Transport." + context.Request.Method);
    context.Response.Headers["X-Trace-Id"] = Activity.Current?.TraceId.ToString();
    await next(context);
    if (context.GetEndpoint() is Microsoft.AspNetCore.Routing.RouteEndpoint route)
    {
        if (span is not null)
            span.DisplayName = "Transport." + context.Request.Method + " " + route.RoutePattern.RawText;
        span?.SetTag("http.route", route.RoutePattern.RawText);
    }
});
app.UseForwardedIdentityHeaderGuard();
if (enableOpenApi && anonymousDescriptionSurface)
{
    // The UI is a static shell that a browser navigates to, so it cannot carry a bearer token, and
    // the authenticated fallback policy applies to middleware-served content as well as to mapped
    // endpoints. It is therefore mounted ahead of authentication and only in Development. Outside
    // Development it is not served at all; the document endpoint remains and stays protected.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FleetCompany.FleetManagement v1");
        options.RoutePrefix = "openapi-ui";
    });
}

app.UseRouting();
// Runs after routing so the resolved endpoint's listener binding can be checked, and before
// authentication so a misrouted call is refused without evaluating any credential.
app.UseTransportPortSeparation();
app.UseAuthentication();
app.UseAuthorization();
await LocalDatabaseBootstrap.RunAsync(app);
var grpcProbeEndpoint = app.MapGrpcService<PlatformProbeService>();
var operationsGrpcEndpoint = app.MapGrpcService<OperationsMissionsService>().RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("Operator"));
var fleetVehiclesGrpcEndpoint = app.MapGrpcService<FleetVehiclesService>().RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager", "Operator"));
var grpcHealthEndpoint = app.MapGrpcHealthChecksService();
if (allowAnonymousHealthEndpoints)
{
    grpcHealthEndpoint.AllowAnonymous();
}

IEndpointConventionBuilder? grpcReflection = null;
if (enableGrpcReflection)
{
    // Reflection lets grpcui and grpcurl discover services without a local .proto copy. Outside
    // Development it stays behind the authenticated fallback, so enabling it on a shared host does not
    // hand the service inventory to an anonymous caller.
    grpcReflection = app.MapGrpcReflectionService();
    if (anonymousDescriptionSurface)
    {
        grpcReflection.AllowAnonymous();
    }
}

IEndpointConventionBuilder? openApiDocument = null;
if (enableOpenApi)
{
    // The document describes the whole REST surface. Anonymous only in Development; when enabled
    // elsewhere it exists but requires a token like every other endpoint.
    openApiDocument = app.MapOpenApi();
    if (anonymousDescriptionSurface)
    {
        openApiDocument.AllowAnonymous();
    }
}

var restProbeEndpoints = app.MapPlatformProbeEndpoints();
var vehicleEndpoints = app.MapVehicleEndpoints();
var driverEndpoints = app.MapDriverEndpoints();
var missionEndpoints = app.MapMissionEndpoints();
var auditEndpoints = app.MapAuditEndpoints();
var livenessEndpoint = app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = static check => check.Tags.Contains(HostHealthChecks.Live), ResponseWriter = WriteAggregateStatusAsync });
var readinessEndpoint = app.MapHealthChecks("/health/ready", new HealthCheckOptions { ResponseWriter = WriteAggregateStatusAsync });
var startupEndpoint = app.MapHealthChecks("/health/startup", new HealthCheckOptions { ResponseWriter = WriteAggregateStatusAsync });
if (allowAnonymousHealthEndpoints)
{
    livenessEndpoint.AllowAnonymous();
    readinessEndpoint.AllowAnonymous();
    startupEndpoint.AllowAnonymous();
}

IEndpointConventionBuilder? metricsScrape = null;
if (metricsScrapeEnabled)
{
    metricsScrape = app.MapMPCorePrometheusScrape(app.Configuration.GetValue("Observability:Metrics:Prometheus:Path", "/metrics")!);
}

// Endpoints are bound to the Kestrel listener they may be served from, never to the client-supplied
// Host header. TransportEndpointGuard has already proved both ports match real listeners.
if (app.Configuration.GetValue("Transport:EnforcePortSeparation", true))
{
    var restPort = app.Configuration.GetValue("Transport:RestPort", 8080);
    var grpcPort = app.Configuration.GetValue("Transport:GrpcPort", 8081);
    grpcProbeEndpoint.RequireListenerPort(grpcPort);
    operationsGrpcEndpoint.RequireListenerPort(grpcPort);
    fleetVehiclesGrpcEndpoint.RequireListenerPort(grpcPort);
    grpcHealthEndpoint.RequireListenerPort(grpcPort);
    restProbeEndpoints.RequireListenerPort(restPort);
    vehicleEndpoints.RequireListenerPort(restPort);
    driverEndpoints.RequireListenerPort(restPort);
    missionEndpoints.RequireListenerPort(restPort);
    auditEndpoints.RequireListenerPort(restPort);
    livenessEndpoint.RequireListenerPort(restPort);
    readinessEndpoint.RequireListenerPort(restPort);
    startupEndpoint.RequireListenerPort(restPort);
    openApiDocument?.RequireListenerPort(restPort);
    metricsScrape?.RequireListenerPort(restPort);
    grpcReflection?.RequireListenerPort(grpcPort);
}

await app.RunAsync();
// Health responses expose the aggregate status word only. Check names, dependency hosts, durations
// and exception text are never disclosed anonymously.
static Task WriteAggregateStatusAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "text/plain; charset=utf-8";
    return context.Response.WriteAsync(report.Status.ToString());
}

public partial class Program;
