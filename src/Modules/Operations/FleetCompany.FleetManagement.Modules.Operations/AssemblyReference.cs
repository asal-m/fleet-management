using System.Reflection;

namespace FleetCompany.FleetManagement.Modules.Operations;

public static class AssemblyReference
{
    public static Assembly Assembly { get; } =
        typeof(AssemblyReference).Assembly;
}