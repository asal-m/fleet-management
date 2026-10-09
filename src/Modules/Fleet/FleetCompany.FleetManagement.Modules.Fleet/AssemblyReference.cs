using System.Reflection;

namespace FleetCompany.FleetManagement.Modules.Fleet;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } = typeof(AssemblyReference).Assembly;
}
