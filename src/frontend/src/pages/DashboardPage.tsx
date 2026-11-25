import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getDashboard } from '../api/management'
import { getApiErrorMessage } from '../api/httpClient'
import { useAuth } from '../auth/AuthContext'

const currency = new Intl.NumberFormat('es-DO', { style: 'currency', currency: 'DOP' })

function currentUtcDate() {
  return new Date().toISOString().slice(0, 10)
}

function monthStartUtc() {
  const today = currentUtcDate()
  return `${today.slice(0, 8)}01`
}

export function DashboardPage() {
  const { user } = useAuth()
  const [fromDate, setFromDate] = useState(monthStartUtc)
  const [toDate, setToDate] = useState(currentUtcDate)
  const query = useQuery({
    queryKey: ['dashboard', fromDate, toDate],
    queryFn: () => getDashboard({ fromDate, toDate }),
  })
  const canSeeSales = user?.roles.some((role) => role === 'Administrador' || role === 'Empleado') ?? false
  const canSeeInventory = user?.roles.some((role) => role === 'Administrador' || role === 'EncargadoAlmacen') ?? false

  return (
    <section className="catalog-page">
      <div className="page-heading">
        <div><p className="eyebrow">Inicio</p><h1>Resumen del negocio</h1><p className="page-description">Indicadores consultados desde la API para el período seleccionado.</p></div>
      </div>

      <div className="catalog-toolbar analytics-period">
        <label>Desde<input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} /></label>
        <label>Hasta<input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} /></label>
      </div>

      {query.isPending ? <p className="catalog-message" role="status">Cargando indicadores…</p>
        : query.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(query.error)}</p>
          : <>
            {canSeeSales && <div className="analytics-cards">
              <article className="analytics-card"><span>Ventas de hoy</span><strong>{currency.format(query.data.todaySalesTotal ?? 0)}</strong><small>{query.data.todaySaleCount ?? 0} ventas completadas</small></article>
              <article className="analytics-card"><span>Ventas del período</span><strong>{currency.format(query.data.salesTotal ?? 0)}</strong><small>{query.data.saleCount ?? 0} ventas completadas</small></article>
            </div>}
            {canSeeInventory && <section className="analytics-panel">
              <div className="section-heading-row"><div><p className="eyebrow">Inventario</p><h2 className="section-title">Existencias en mínimo o por debajo</h2></div><Link className="text-button" to="/inventory">Ver inventario</Link></div>
              <p className="analytics-count">{query.data.lowStockCount ?? 0} productos requieren revisión</p>
              {query.data.lowStockProducts.length === 0 ? <p className="catalog-message">No hay productos activos en el mínimo o por debajo.</p>
                : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Producto</th><th>Existencia</th><th>Mínimo</th></tr></thead><tbody>
                  {query.data.lowStockProducts.map((product) => <tr key={product.productId}><td><strong>{product.name}</strong><small>SKU {product.sku}</small></td><td className="stock-low">{product.currentStock}</td><td>{product.minimumStock}</td></tr>)}
                </tbody></table></div>}
            </section>}
            {canSeeSales && <section className="analytics-panel">
              <div className="section-heading-row"><div><p className="eyebrow">Ventas</p><h2 className="section-title">Productos más vendidos</h2></div><Link className="text-button" to="/sales">Abrir punto de venta</Link></div>
              {query.data.topProducts.length === 0 ? <p className="catalog-message">No hay ventas completadas en el período.</p>
                : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Producto</th><th>Unidades</th><th>Total vendido</th></tr></thead><tbody>
                  {query.data.topProducts.map((product) => <tr key={product.productId}><td><strong>{product.name}</strong><small>SKU {product.sku}</small></td><td>{product.quantity}</td><td>{currency.format(product.total)}</td></tr>)}
                </tbody></table></div>}
            </section>}
          </>}
    </section>
  )
}
