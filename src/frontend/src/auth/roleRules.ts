export const catalogManagerRoles = ['Administrador', 'EncargadoAlmacen']
export const salesRoles = ['Administrador', 'Empleado']
export const administratorRoles = ['Administrador']

export function hasCatalogManagementRole(roles: string[] | undefined): boolean {
  return Boolean(roles?.some((role) => catalogManagerRoles.includes(role)))
}
