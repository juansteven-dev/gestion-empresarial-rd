import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getMovements, getProducts } from '../api/catalog'
import { getReports } from '../api/management'
import { getSales } from '../api/sales'
import { getApiErrorMessage } from '../api/httpClient'

const currency = new Intl.NumberFormat('es-DO', { style: 'currency', currency: 'DOP' })
const movementLabels = ['Compra', 'Venta', 'Ajuste']

function currentUtcDate() {
  return new Date().toISOString().slice(0, 10)
}

function monthStartUtc() {
  const today = currentUtcDate()
  return `${today.slice(0, 8)}01`
}

export function ReportsPage() {
  const [fromDate, setFromDate] = useState(monthStartUtc)
  const [toDate, setToDate] = useState(currentUtcDate)
  const [salesSearch, setSalesSearch] = useState('')
  const [salesPage, setSalesPage] = useState(1)
  const [productSearch, setProductSearch] = useState('')
  const [movementProductId, setMovementProductId] = useState('')
  const [movementPage, setMovementPage] = useState(1)
  const query = useQuery({
    queryKey: ['reports', fromDate, toDate],
    queryFn: () => getReports({ fromDate, toDate }),
  })
  const salesQuery = useQuery({
    queryKey: ['reports-sales', fromDate, toDate, salesSearch, salesPage],
    queryFn: () => getSales({ fromDate, toDate, search: salesSearch, page: salesPage, pageSize: 10 }),
  })
  const productsQuery = useQuery({
    queryKey: ['reports-movement-products', productSearch],
    queryFn: () => getProducts({ search: productSearch, page: 1, pageSize: 25 }),
  })
  const movementsQuery = useQuery({
    queryKey: ['reports-movements', fromDate, toDate, movementProductId, movementPage],
    queryFn: () => getMovements({
      productId: movementProductId ? Number(movementProductId) : undefined,
      fromUtc: `${fromDate}T00:00:00.0000000Z`,
      toUtc: `${toDate}T23:59:59.9999999Z`,
      page: movementPage,
      pageSize: 10,
    }),
  })

  return (
    <section className="catalog-page">
      <div className="page-heading"><div><p className="eyebrow">Análisis</p><h1>Reportes</h1><p className="page-description">Ventas completadas, productos vendidos y actividad de inventario en el período.</p></div></div>
      <div className="catalog-toolbar analytics-period"><label>Desde<input type="date" value={fromDate} onChange={(event) => { setFromDate(event.target.value); setSalesPage(1); setMovementPage(1) }} /></label><label>Hasta<input type="date" value={toDate} onChange={(event) => { setToDate(event.target.value); setSalesPage(1); setMovementPage(1) }} /></label></div>
      {query.isPending ? <p className="catalog-message" role="status">Cargando reportes…</p>
        : query.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(query.error)}</p>
          : <>
            <div className="analytics-cards">
              <article className="analytics-card"><span>Ventas del período</span><strong>{currency.format(query.data.salesTotal)}</strong></article>
              <article className="analytics-card"><span>Número de ventas</span><strong>{query.data.saleCount}</strong></article>
            </div>
            {query.data.salesByDay.length === 0 && query.data.topProducts.length === 0 && query.data.movements.length === 0
              ? <p className="catalog-message">No hay actividad para el período seleccionado.</p>
              : <div className="reports-grid">
                <section className="analytics-panel report-chart"><h2 className="section-title">Ventas por día</h2>
                  {query.data.salesByDay.length === 0 ? <p className="catalog-message">No hay ventas en el período.</p>
                    : <div className="chart-frame"><ResponsiveContainer width="100%" height="100%"><BarChart data={query.data.salesByDay.map((item) => ({ ...item, day: item.dayUtc.slice(0, 10) }))} margin={{ top: 12, right: 12, bottom: 4, left: 12 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} /><XAxis dataKey="day" /><YAxis /><Tooltip formatter={(value) => currency.format(Number(value))} /><Bar dataKey="total" name="Ventas" fill="#4d806f" radius={[5, 5, 0, 0]} />
                    </BarChart></ResponsiveContainer></div>}
                </section>
                <section className="analytics-panel report-chart"><h2 className="section-title">Unidades por producto</h2>
                  {query.data.topProducts.length === 0 ? <p className="catalog-message">No hay productos vendidos en el período.</p>
                    : <div className="chart-frame"><ResponsiveContainer width="100%" height="100%"><BarChart data={query.data.topProducts} layout="vertical" margin={{ top: 12, right: 16, bottom: 4, left: 8 }}>
                      <CartesianGrid strokeDasharray="3 3" horizontal={false} /><XAxis type="number" /><YAxis type="category" dataKey="name" width={120} /><Tooltip /><Bar dataKey="quantity" name="Unidades" fill="#547ea2" radius={[0, 5, 5, 0]} />
                    </BarChart></ResponsiveContainer></div>}
                </section>
              </div>}
            <section className="analytics-panel"><h2 className="section-title">Movimientos de inventario</h2>
              {query.data.movements.length === 0 ? <p className="catalog-message">No hay movimientos en el período.</p>
                : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Tipo</th><th>Cantidad de movimientos</th><th>Variación neta</th></tr></thead><tbody>
                  {query.data.movements.map((movement) => <tr key={movement.type}><td>{movementLabels[movement.type] ?? 'Movimiento'}</td><td>{movement.movementCount}</td><td>{movement.quantityDelta > 0 ? '+' : ''}{movement.quantityDelta}</td></tr>)}
                </tbody></table></div>}
            </section>
          </>}

      <section className="analytics-panel">
        <div className="section-heading-row"><div><p className="eyebrow">Detalle</p><h2 className="section-title">Ventas registradas</h2></div></div>
        <div className="catalog-toolbar"><label className="toolbar-search">Filtrar por número de venta<input value={salesSearch} onChange={(event) => { setSalesSearch(event.target.value); setSalesPage(1) }} /></label></div>
        {salesQuery.isPending ? <p className="catalog-message" role="status">Cargando ventas…</p>
          : salesQuery.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(salesQuery.error)}</p>
            : salesQuery.data.items.length === 0 ? <p className="catalog-message">No hay ventas para los filtros seleccionados.</p>
              : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Fecha</th><th>Número</th><th>Líneas</th><th>Total</th></tr></thead><tbody>
                {salesQuery.data.items.map((sale) => <tr key={sale.saleId}><td>{new Date(sale.createdAtUtc).toLocaleString()}</td><td>{sale.saleNumber}</td><td>{sale.itemCount}</td><td>{currency.format(sale.total)}</td></tr>)}
              </tbody></table></div>}
        {salesQuery.data && <div className="pagination"><span>{salesQuery.data.totalCount} ventas · página {salesQuery.data.page} de {Math.max(salesQuery.data.totalPages, 1)}</span><div><button className="secondary-button" type="button" disabled={salesPage <= 1} onClick={() => setSalesPage((value) => value - 1)}>Anterior</button><button className="secondary-button" type="button" disabled={salesPage >= salesQuery.data.totalPages} onClick={() => setSalesPage((value) => value + 1)}>Siguiente</button></div></div>}
      </section>

      <section className="analytics-panel">
        <div className="section-heading-row"><div><p className="eyebrow">Inventario</p><h2 className="section-title">Movimientos por producto y fecha</h2></div></div>
        <div className="catalog-toolbar">
          <label>Buscar producto<input value={productSearch} onChange={(event) => { setProductSearch(event.target.value); setMovementProductId(''); setMovementPage(1) }} /></label>
          <label>Producto<select value={movementProductId} onChange={(event) => { setMovementProductId(event.target.value); setMovementPage(1) }}><option value="">Todos los productos</option>{productsQuery.data?.items.map((product) => <option key={product.productId} value={product.productId}>{product.name} · {product.sku}</option>)}</select></label>
        </div>
        {productsQuery.isPending && <p className="catalog-message" role="status">Cargando opciones de producto…</p>}
        {productsQuery.isError && <p className="catalog-message error-text" role="alert">{getApiErrorMessage(productsQuery.error)}</p>}
        {productsQuery.data && productsQuery.data.items.length === 0 && <p className="catalog-message">No hay productos que coincidan con la búsqueda.</p>}
        {movementsQuery.isPending ? <p className="catalog-message" role="status">Cargando movimientos…</p>
          : movementsQuery.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(movementsQuery.error)}</p>
            : movementsQuery.data.items.length === 0 ? <p className="catalog-message">No hay movimientos para los filtros seleccionados.</p>
              : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Fecha</th><th>Producto</th><th>Tipo</th><th>Cambio</th><th>Existencia</th><th>Referencia</th></tr></thead><tbody>
                {movementsQuery.data.items.map((movement) => <tr key={movement.stockMovementId}><td>{new Date(movement.createdAtUtc).toLocaleString()}</td><td><strong>{movement.productName}</strong><small>SKU {movement.sku}</small></td><td>{movementLabels[movement.type] ?? 'Movimiento'}</td><td>{movement.quantityDelta > 0 ? '+' : ''}{movement.quantityDelta}</td><td>{movement.stockBefore} → {movement.stockAfter}</td><td>{movement.notes || '—'}</td></tr>)}
              </tbody></table></div>}
        {movementsQuery.data && <div className="pagination"><span>{movementsQuery.data.totalCount} movimientos · página {movementsQuery.data.page} de {Math.max(movementsQuery.data.totalPages, 1)}</span><div><button className="secondary-button" type="button" disabled={movementPage <= 1} onClick={() => setMovementPage((value) => value - 1)}>Anterior</button><button className="secondary-button" type="button" disabled={movementPage >= movementsQuery.data.totalPages} onClick={() => setMovementPage((value) => value + 1)}>Siguiente</button></div></div>}
      </section>
    </section>
  )
}

export default ReportsPage
