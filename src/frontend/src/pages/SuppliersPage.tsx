import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createSupplier, deactivateSupplier, getSuppliers, updateSupplier, type Supplier } from '../api/catalog'
import { getApiErrorMessage } from '../api/httpClient'

export function SuppliersPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [activeFilter, setActiveFilter] = useState('true')
  const [page, setPage] = useState(1)
  const [editing, setEditing] = useState<Supplier | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const query = useQuery({
    queryKey: ['suppliers', search, activeFilter, page],
    queryFn: () => getSuppliers({ search, isActive: activeFilter === '' ? undefined : activeFilter === 'true', page, pageSize: 10 }),
  })
  const saveMutation = useMutation({
    mutationFn: async (values: { name: string; taxId: string; phone: string; email: string; address: string; isActive: boolean }) =>
      editing ? updateSupplier(editing.supplierId, values) : createSupplier(values),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['suppliers'] })
      setFormOpen(false)
      setEditing(null)
    },
  })
  const deactivateMutation = useMutation({
    mutationFn: deactivateSupplier,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['suppliers'] }),
  })

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    saveMutation.mutate({
      name: String(values.get('name') ?? '').trim(),
      taxId: String(values.get('taxId') ?? '').trim(),
      phone: String(values.get('phone') ?? '').trim(),
      email: String(values.get('email') ?? '').trim(),
      address: String(values.get('address') ?? '').trim(),
      isActive: values.get('isActive') === 'true',
    })
  }

  return (
    <section className="catalog-page">
      <div className="page-heading">
        <div><p className="eyebrow">Abastecimiento</p><h1>Suplidores</h1><p className="page-description">Mantén la información de contacto y desactiva los suplidores que ya no estén activos.</p></div>
        <button className="primary-button" type="button" onClick={() => { setEditing(null); setFormOpen(true); saveMutation.reset() }}>Nuevo suplidor</button>
      </div>

      {formOpen && <form className="catalog-form" key={editing?.supplierId ?? 'new'} onSubmit={submit}>
        <div className="form-heading"><h2>{editing ? 'Editar suplidor' : 'Crear suplidor'}</h2><button className="text-button" type="button" onClick={() => setFormOpen(false)}>Cerrar</button></div>
        <div className="form-grid">
          <label>Nombre<input name="name" required minLength={2} maxLength={120} defaultValue={editing?.name ?? ''} /></label>
          <label>RNC / identificación<input name="taxId" maxLength={20} defaultValue={editing?.taxId ?? ''} /></label>
          <label>Teléfono<input name="phone" maxLength={30} defaultValue={editing?.phone ?? ''} /></label>
          <label>Correo<input name="email" type="email" maxLength={254} defaultValue={editing?.email ?? ''} /></label>
          <label>Estado<select name="isActive" defaultValue={String(editing?.isActive ?? true)}><option value="true">Activo</option><option value="false">Inactivo</option></select></label>
          <label className="form-wide">Dirección<input name="address" maxLength={250} defaultValue={editing?.address ?? ''} /></label>
        </div>
        {saveMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(saveMutation.error)}</p>}
        <button className="primary-button" type="submit" disabled={saveMutation.isPending}>{saveMutation.isPending ? 'Guardando…' : 'Guardar suplidor'}</button>
      </form>}

      <div className="catalog-toolbar">
        <label className="toolbar-search">Buscar<input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="Nombre, RNC o correo" /></label>
        <label>Estado<select value={activeFilter} onChange={(event) => { setActiveFilter(event.target.value); setPage(1) }}><option value="true">Activos</option><option value="false">Inactivos</option><option value="">Todos</option></select></label>
      </div>

      {query.isPending ? <p className="catalog-message">Cargando suplidores…</p>
        : query.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(query.error)}</p>
          : query.data.items.length === 0 ? <p className="catalog-message">No hay suplidores que coincidan con la búsqueda.</p>
            : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Suplidor</th><th>Identificación</th><th>Contacto</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>
              {query.data.items.map((supplier) => <tr key={supplier.supplierId}>
                <td><strong>{supplier.name}</strong><small>{supplier.address || 'Sin dirección registrada'}</small></td><td>{supplier.taxId || '—'}</td><td>{supplier.email || '—'}<small>{supplier.phone || 'Sin teléfono'}</small></td><td><span className={`status-pill ${supplier.isActive ? 'active' : 'inactive'}`}>{supplier.isActive ? 'Activo' : 'Inactivo'}</span></td>
                <td className="row-actions"><button className="text-button" type="button" onClick={() => { setEditing(supplier); setFormOpen(true); saveMutation.reset() }}>Editar</button>{supplier.isActive && <button className="text-button danger-text" type="button" disabled={deactivateMutation.isPending} onClick={() => deactivateMutation.mutate(supplier.supplierId)}>Desactivar</button>}</td>
              </tr>)}
            </tbody></table></div>}
      {deactivateMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(deactivateMutation.error)}</p>}
      {query.data && <div className="pagination"><span>{query.data.totalCount} suplidores · página {query.data.page} de {Math.max(query.data.totalPages, 1)}</span><div><button className="secondary-button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button><button className="secondary-button" disabled={page >= query.data.totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button></div></div>}
    </section>
  )
}
