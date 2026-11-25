import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

export const catalogManagerRoles = ['Administrador', 'EncargadoAlmacen']
export const salesRoles = ['Administrador', 'Empleado']

export function hasCatalogManagementRole(roles: string[] | undefined): boolean {
  return Boolean(roles?.some((role) => catalogManagerRoles.includes(role)))
}

export function RequireRoles({ roles }: { roles: string[] }) {
  const { user } = useAuth()

  return roles.some((role) => user?.roles.includes(role))
    ? <Outlet />
    : <Navigate to="/products" replace />
}
