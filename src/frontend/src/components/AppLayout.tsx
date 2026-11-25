import { useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { getApiErrorMessage } from '../api/httpClient'
import { useAuth } from '../auth/AuthContext'

const navigationItems = [
  { to: '/dashboard', label: 'Resumen', end: true },
  { to: '/products', label: 'Productos' },
  { to: '/purchases', label: 'Compras' },
  { to: '/sales', label: 'Ventas' },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [logoutError, setLogoutError] = useState('')
  const [isLoggingOut, setIsLoggingOut] = useState(false)

  async function handleLogout() {
    setLogoutError('')
    setIsLoggingOut(true)

    try {
      await logout()
      navigate('/login', { replace: true })
    } catch (error) {
      setLogoutError(getApiErrorMessage(error))
    } finally {
      setIsLoggingOut(false)
    }
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <Link className="brand" to="/dashboard" aria-label="Gestión Empresarial RD, inicio">
          <span className="brand-mark" aria-hidden="true">GE</span>
          <span className="brand-name">Gestión Empresarial <strong>RD</strong></span>
        </Link>

        <p className="nav-heading">GESTIÓN</p>
        <nav className="primary-nav" aria-label="Navegación principal">
          {navigationItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => `nav-link${isActive ? ' is-active' : ''}`}
            >
              <span className="nav-indicator" aria-hidden="true" />
              {item.label}
            </NavLink>
          ))}
        </nav>

        <p className="sidebar-note">Base inicial del sistema</p>
      </aside>

      <div className="main-area">
        <header className="topbar">
          <div>
            <p className="topbar-label">ESPACIO DE TRABAJO</p>
            <p className="topbar-title">Operación local</p>
          </div>
          <div className="topbar-actions">
            {logoutError && <p className="logout-error" role="alert">{logoutError}</p>}
            <span className="topbar-user">{user?.userName}</span>
            <button className="logout-button" type="button" onClick={handleLogout} disabled={isLoggingOut}>
              {isLoggingOut ? 'Saliendo…' : 'Cerrar sesión'}
            </button>
          </div>
        </header>

        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
