import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { RequireAuth } from './auth/RequireAuth'
import { catalogManagerRoles, RequireRoles } from './auth/RequireRoles'
import { AppLayout } from './components/AppLayout'
import { PlaceholderPage } from './components/PlaceholderPage'
import { CategoriesPage } from './pages/CategoriesPage'
import { InventoryPage } from './pages/InventoryPage'
import { LoginPage } from './pages/LoginPage'
import { ProductsPage } from './pages/ProductsPage'
import { SuppliersPage } from './pages/SuppliersPage'
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
            </Route>
            <Route
              path="purchases"
              element={
                <PlaceholderPage
                  section="Abastecimiento"
                  title="Compras"
                  description="Esta ruta esta preparada para el futuro modulo de compras."
                />
              }
            />
            <Route
              path="sales"
              element={
                <PlaceholderPage
                  section="Operaciones"
                  title="Ventas"
                  description="Esta ruta esta preparada para el futuro modulo de ventas."
                />
              }
            />
          </Route>
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
