using MPCore.Domain.Model;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

public sealed class MissionLocation : ValueObject
{
    public string Value { get; }
    private MissionLocation(string? value)
    {
        var text = value?.Trim();
        BusinessRules.Check(new MissionLocationMustBeValidRule(text));
        Value = text!;
    }
    public static MissionLocation Create(string? value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
