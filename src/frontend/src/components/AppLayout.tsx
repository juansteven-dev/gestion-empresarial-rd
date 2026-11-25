import { Link, NavLink, Outlet } from 'react-router-dom'

const navigationItems = [
  { to: '/dashboard', label: 'Resumen', end: true },
  { to: '/products', label: 'Productos' },
  { to: '/purchases', label: 'Compras' },
  { to: '/sales', label: 'Ventas' },
]

export function AppLayout() {
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
          <NavLink className="login-link" to="/login">Iniciar sesión</NavLink>
        </header>

        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
