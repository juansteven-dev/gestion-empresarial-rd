import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getAuditLogs } from '../api/management'
import { getApiErrorMessage } from '../api/httpClient'

export function AuditPage() {
  const [search, setSearch] = useState('')
  const [userId, setUserId] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [page, setPage] = useState(1)
  const query = useQuery({
    queryKey: ['audit', search, userId, fromDate, toDate, page],
    queryFn: () => getAuditLogs({ search, userId, fromDate: fromDate || undefined, toDate: toDate || undefined, page, pageSize: 15 }),
  })

  return (
    <section className="catalog-page">
      <div className="page-heading"><div><p className="eyebrow">Administración</p><h1>Auditoría</h1><p className="page-description">Consulta quién realizó acciones sensibles y cuándo ocurrieron.</p></div></div>
      <div className="catalog-toolbar">
        <label className="toolbar-search">Acción o entidad<input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="ProductCreated, Sale…" /></label>
        <label>Usuario ID<input value={userId} onChange={(event) => { setUserId(event.target.value); setPage(1) }} /></label>
        <label>Desde<input type="date" value={fromDate} onChange={(event) => { setFromDate(event.target.value); setPage(1) }} /></label>
        <label>Hasta<input type="date" value={toDate} onChange={(event) => { setToDate(event.target.value); setPage(1) }} /></label>
      </div>
      {query.isPending ? <p className="catalog-message" role="status">Cargando registros de auditoría…</p>
        : query.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(query.error)}</p>
          : query.data.items.length === 0 ? <p className="catalog-message">No hay registros para los filtros seleccionados.</p>
            : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Fecha</th><th>Usuario</th><th>Acción</th><th>Entidad</th><th>Detalle</th><th>IP</th></tr></thead><tbody>
              {query.data.items.map((entry) => <tr key={entry.auditLogId}>
                <td>{new Date(entry.createdAtUtc).toLocaleString()}</td><td><strong>{entry.userName}</strong><small>{entry.userId}</small></td><td>{entry.action}</td><td>{entry.entityName}{entry.entityId ? ` · ${entry.entityId}` : ''}</td>
                <td>{entry.detailsJson ? <details><summary>Ver JSON</summary><pre className="audit-details">{entry.detailsJson}</pre></details> : '—'}</td><td>{entry.ipAddress || '—'}</td>
              </tr>)}
            </tbody></table></div>}
      {query.data && <div className="pagination"><span>{query.data.totalCount} registros · página {query.data.page} de {Math.max(query.data.totalPages, 1)}</span><div><button className="secondary-button" type="button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button><button className="secondary-button" type="button" disabled={page >= query.data.totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button></div></div>}
    </section>
  )
}
