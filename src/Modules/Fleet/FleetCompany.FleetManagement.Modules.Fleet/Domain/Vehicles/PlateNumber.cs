using MPCore.Domain.Model;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

public sealed class PlateNumber : ValueObject
{
    public string Value { get; }

    private PlateNumber(string? value)
    {
        var normalized = Normalize(value);
        BusinessRules.Check(new PlateNumberMustBeValidRule(normalized));
        Value = normalized!;
    }

    public static PlateNumber Create(string? value) => new(value);
    private static string? Normalize(string? value)
    {
        if (value is null)
            return null;
        var characters = value.Trim().ToUpperInvariant().ToCharArray();
        for (var index = 0; index < characters.Length; index++)
        {
            var character = characters[index];
            if (character is >= '\u06F0' and <= '\u06F9')
                characters[index] = (char)('0' + character - '\u06F0');
            else if (character is >= '\u0660' and <= '\u0669')
                characters[index] = (char)('0' + character - '\u0660');
        }

        return new string(characters);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
