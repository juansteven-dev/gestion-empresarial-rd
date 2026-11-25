import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { RequireAuth } from './auth/RequireAuth'
import { AppLayout } from './components/AppLayout'
import { PlaceholderPage } from './components/PlaceholderPage'
import { LoginPage } from './pages/LoginPage'
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
                  description="Aquí se mostrarán los indicadores principales cuando se implemente el módulo de reportes."
                />
              }
            />
            <Route
              path="products"
              element={
                <PlaceholderPage
                  section="Catálogo"
                  title="Productos"
                  description="La consulta y administración del catálogo se implementará en su fase correspondiente."
                />
              }
            />
            <Route
              path="purchases"
              element={
                <PlaceholderPage
                  section="Abastecimiento"
                  title="Compras"
                  description="Esta ruta está preparada para el futuro módulo de compras."
                />
              }
            />
            <Route
              path="sales"
              element={
                <PlaceholderPage
                  section="Operaciones"
                  title="Ventas"
                  description="Esta ruta está preparada para el futuro módulo de ventas."
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
