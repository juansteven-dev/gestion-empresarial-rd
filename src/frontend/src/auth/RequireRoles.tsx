import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function RequireRoles({ roles }: { roles: string[] }) {
  const { user } = useAuth()

  return roles.some((role) => user?.roles.includes(role))
    ? <Outlet />
    : <Navigate to="/products" replace />
}
