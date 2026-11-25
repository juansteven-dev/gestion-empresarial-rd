namespace GestionEmpresarial.Api.Authorization;

public static class RoleNames
{
    public const string Administrator = "Administrador";
    public const string Employee = "Empleado";
    public const string WarehouseManager = "EncargadoAlmacen";

    public static IReadOnlyList<string> All { get; } =
    [
        Administrator,
        Employee,
        WarehouseManager
    ];

    public static bool IsKnown(string role) => All.Contains(role, StringComparer.Ordinal);
}
