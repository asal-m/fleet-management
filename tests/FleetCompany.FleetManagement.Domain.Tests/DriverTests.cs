using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
using MPCore.Domain.Rules;
using Xunit;

namespace FleetCompany.FleetManagement.Domain.Tests;

public sealed class DriverTests
{
    [Fact]
    public void Registration_preserves_profile_and_deduplicates_normalized_qualifications()
    {
        var codes = new List<QualificationCode>
        {
            QualificationCode.Create(" truck "),
            QualificationCode.Create("TRUCK"),
            QualificationCode.Create("van")
        };
        var driver = Register(DriverStatus.Active, codes);
        codes.Clear();
        Assert.Equal("فاطمه", driver.FirstName.Value);
        Assert.Equal("Mozafari", driver.LastName.Value);
        Assert.True(driver.IsActive);
        Assert.Equal(2, driver.Qualifications.Count);
        Assert.True(driver.IsQualifiedFor(QualificationCode.Create("TrUcK")));
        Assert.False(driver.IsQualifiedFor(QualificationCode.Create("BUS")));
        Assert.False(driver.IsQualifiedFor(QualificationCode.Create("TRUK")));
    }

    [Fact]
    public void Inactive_driver_retains_qualifications_but_is_not_active()
    {
        var driver = Register(DriverStatus.Inactive, [QualificationCode.Create("BUS")]);
        Assert.False(driver.IsActive);
        Assert.True(driver.IsQualifiedFor(QualificationCode.Create("BUS")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Undefined_status_is_rejected(int status) => Assert.Throws<BusinessRuleValidationException>(() => Register((DriverStatus)status, [QualificationCode.Create("BUS")]));
    [Fact]
    public void Empty_or_null_qualification_entries_are_rejected()
    {
        Assert.Throws<BusinessRuleValidationException>(() => Register(DriverStatus.Active, []));
        Assert.Throws<BusinessRuleValidationException>(() => Register(DriverStatus.Active, [null!]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("TRUCK-BUS")]
    [InlineData("TRUCK BUS")]
    [InlineData("کامیون")]
    public void Malformed_qualification_codes_are_rejected(string? code) => Assert.Throws<BusinessRuleValidationException>(() => QualificationCode.Create(code));
    [Fact]
    public void Name_and_code_length_boundaries_are_enforced_after_trimming()
    {
        Assert.Equal(100, DriverName.Create(" " + new string('ن', 100) + " ").Value.Length);
        Assert.Throws<BusinessRuleValidationException>(() => DriverName.Create(new string('ن', 101)));
        Assert.Equal(50, QualificationCode.Create(new string('A', 50)).Value.Length);
        Assert.Throws<BusinessRuleValidationException>(() => QualificationCode.Create(new string('A', 51)));
        Assert.Equal("CAR_123", QualificationCode.Create(" car_123 ").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Empty_names_are_rejected(string? name) => Assert.Throws<BusinessRuleValidationException>(() => DriverName.Create(name));
    private static Driver Register(DriverStatus status, IEnumerable<QualificationCode> codes) => Driver.Register(Guid.NewGuid(), DriverName.Create(" فاطمه "), DriverName.Create(" Mozafari "), status, codes);
}
