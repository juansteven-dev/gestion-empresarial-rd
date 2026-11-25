import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { RequireAuth } from './auth/RequireAuth'
import { catalogManagerRoles, RequireRoles, salesRoles } from './auth/RequireRoles'
import { AppLayout } from './components/AppLayout'
import { PlaceholderPage } from './components/PlaceholderPage'
import { CategoriesPage } from './pages/CategoriesPage'
import { InventoryPage } from './pages/InventoryPage'
import { LoginPage } from './pages/LoginPage'
import { ProductsPage } from './pages/ProductsPage'
import { PurchasesPage } from './pages/PurchasesPage'
import { SuppliersPage } from './pages/SuppliersPage'
import { SalesPage } from './pages/SalesPage'
import './App.css'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<RequireAuth />}>
          <Route element={<AppLayout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route
              path="dashboard"
              element={
                <PlaceholderPage
                  section="Inicio"
                  title="Resumen del negocio"
                  description="Aqui se mostraran los indicadores principales cuando se implemente el modulo de reportes."
                />
              }
            />
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
          </Route>
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
