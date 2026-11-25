import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getReports } from '../api/management'
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
  const query = useQuery({
    queryKey: ['reports', fromDate, toDate],
    queryFn: () => getReports({ fromDate, toDate }),
  })

  return (
    <section className="catalog-page">
      <div className="page-heading"><div><p className="eyebrow">Análisis</p><h1>Reportes</h1><p className="page-description">Ventas completadas, productos vendidos y actividad de inventario en el período.</p></div></div>
      <div className="catalog-toolbar analytics-period"><label>Desde<input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} /></label><label>Hasta<input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} /></label></div>
      {query.isPending ? <p className="catalog-message" role="status">Cargando reportes…</p>
        : query.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(query.error)}</p>
          : <>
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
    </section>
  )
}

export default ReportsPage
