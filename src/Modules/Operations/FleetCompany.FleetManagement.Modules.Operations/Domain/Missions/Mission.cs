using MPCore.Domain.Model;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

public sealed class Mission : AggregateRoot<Guid>
{
    private static readonly System.Diagnostics.ActivitySource Trace = new("FleetCompany.FleetManagement.Business");
    private Mission() { Origin = null!; Destination = null!; RequiredCapacity = null!; }
    public MissionLocation Origin { get; private set; }
    public MissionLocation Destination { get; private set; }
    public RequiredCapacity RequiredCapacity { get; private set; }
    public DateTimeOffset? ScheduledTime { get; private set; }
    public Guid? AssignedVehicleId { get; private set; }
    public Guid? AssignedDriverId { get; private set; }
    public MissionStatus Status { get; private set; }
    // Reservation ownership follows status; IDs remain as history after completion/cancellation.
    public bool HasActiveReservation => Status is MissionStatus.Assigned or MissionStatus.InProgress;
    private Mission(Guid id, MissionLocation origin, MissionLocation destination, RequiredCapacity capacity) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(capacity);
        Origin = origin;
        Destination = destination;
        RequiredCapacity = capacity;
        Status = MissionStatus.Draft;
    }
    public static Mission Create(Guid id, MissionLocation origin, MissionLocation destination, RequiredCapacity capacity)
        => new(id, origin, destination, capacity);
    // now comes from the Application clock; Domain never reads wall-clock time.
    public bool Schedule(DateTimeOffset scheduledTime, DateTimeOffset now)
    {
        using var domainSpan = Trace.StartActivity("Domain.Mission.Schedule");
        CheckRule(new MissionStateMustAllowOperationRule(Status, MissionStatus.Draft, MissionStatus.Scheduled));
        var utc = scheduledTime.ToUniversalTime();
        utc = new DateTimeOffset(utc.Ticks / 10 * 10, TimeSpan.Zero); // PostgreSQL stores microseconds.
        CheckRule(new ScheduledTimeMustBeInFutureRule(utc, now));
        if (Status == MissionStatus.Scheduled && ScheduledTime == utc) return false;
        ScheduledTime = utc;
        Status = MissionStatus.Scheduled;
        return true;
    }
    // Application supplies trusted resource facts through read-only Contracts under the shared lock.
    // Domain enforces eligibility and lifecycle before mutating this one Aggregate.
    public bool Assign(Guid vehicleId, Guid driverId, AssignmentEligibility eligibility)
    {
        using var domainSpan = Trace.StartActivity("Domain.Mission.Assign");
        CheckRule(new MissionResourceIdsMustBeValidRule(vehicleId, driverId));
        if (Status == MissionStatus.Assigned)
        {
            CheckRule(new AssignedResourcesMustNotChangeRule(AssignedVehicleId, AssignedDriverId, vehicleId, driverId));
            return false;
        }
        CheckRule(new MissionStateMustAllowOperationRule(Status, MissionStatus.Scheduled));
        ArgumentNullException.ThrowIfNull(eligibility);
        CheckRule(new VehicleMustNotBeUnderMaintenanceRule(eligibility.VehicleUnderMaintenance));
        CheckRule(new VehicleMustBeOperationalRule(eligibility.VehicleActive));
        CheckRule(new VehicleCapacityMustSatisfyMissionRule(eligibility.VehicleCapacity, RequiredCapacity.Kilograms));
        CheckRule(new VehicleMustBeUnreservedRule(eligibility.VehicleReserved));
        CheckRule(new DriverMustBeActiveRule(eligibility.DriverActive));
        CheckRule(new DriverMustBeUnreservedRule(eligibility.DriverReserved));
        CheckRule(new DriverMustBeQualifiedRule(eligibility.DriverQualified));
        AssignedVehicleId = vehicleId;
        AssignedDriverId = driverId;
        Status = MissionStatus.Assigned;
        return true;
    }
    public bool Start()
    {
        using var domainSpan = Trace.StartActivity("Domain.Mission.Start");
        if (Status == MissionStatus.InProgress) return false;
        CheckRule(new MissionStateMustAllowOperationRule(Status, MissionStatus.Assigned));
        Status = MissionStatus.InProgress;
        return true;
    }
    public bool Complete()
    {
        using var domainSpan = Trace.StartActivity("Domain.Mission.Complete");
        if (Status == MissionStatus.Completed) return false;
        CheckRule(new MissionStateMustAllowOperationRule(Status, MissionStatus.InProgress));
        Status = MissionStatus.Completed;
        return true;
    }
    public bool Cancel()
    {
        using var domainSpan = Trace.StartActivity("Domain.Mission.Cancel");
        if (Status == MissionStatus.Cancelled) return false;
        CheckRule(new MissionStateMustAllowOperationRule(Status, MissionStatus.Draft, MissionStatus.Scheduled, MissionStatus.Assigned));
        Status = MissionStatus.Cancelled;
        return true;
    }
}
