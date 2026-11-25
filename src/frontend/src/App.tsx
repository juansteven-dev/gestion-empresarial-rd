import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import { RequireAuth } from './auth/RequireAuth'
import { RequireRoles } from './auth/RequireRoles'
import { administratorRoles, catalogManagerRoles, salesRoles } from './auth/roleRules'
import { AppLayout } from './components/AppLayout'
import { AuditPage } from './pages/AuditPage'
import { CategoriesPage } from './pages/CategoriesPage'
import { DashboardPage } from './pages/DashboardPage'
import { InventoryPage } from './pages/InventoryPage'
import { LoginPage } from './pages/LoginPage'
import { ProductsPage } from './pages/ProductsPage'
import { PurchasesPage } from './pages/PurchasesPage'
import { SalesPage } from './pages/SalesPage'
import { SuppliersPage } from './pages/SuppliersPage'
import { UsersPage } from './pages/UsersPage'
import './App.css'

const ReportsPage = lazy(() => import('./pages/ReportsPage'))

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<RequireAuth />}>
          <Route element={<AppLayout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="products" element={<ProductsPage />} />
            <Route element={<RequireRoles roles={catalogManagerRoles} />}>
              <Route path="categories" element={<CategoriesPage />} />
              <Route path="suppliers" element={<SuppliersPage />} />
              <Route path="inventory" element={<InventoryPage />} />
              <Route path="purchases" element={<PurchasesPage />} />
            </Route>
            <Route element={<RequireRoles roles={salesRoles} />}>
              <Route path="sales" element={<SalesPage />} />
            </Route>
            <Route element={<RequireRoles roles={administratorRoles} />}>
              <Route path="reports" element={<Suspense fallback={<p className="catalog-message" role="status">Cargando reportes…</p>}><ReportsPage /></Suspense>} />
              <Route path="users" element={<UsersPage />} />
              <Route path="audit" element={<AuditPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
