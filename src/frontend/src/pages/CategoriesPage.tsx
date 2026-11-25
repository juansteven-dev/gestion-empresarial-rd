import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createCategory, deactivateCategory, getCategories, updateCategory, type Category } from '../api/catalog'
import { getApiErrorMessage } from '../api/httpClient'

export function CategoriesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [activeFilter, setActiveFilter] = useState('true')
  const [page, setPage] = useState(1)
  const [editing, setEditing] = useState<Category | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')
  const query = useQuery({
    queryKey: ['categories', search, activeFilter, page],
    queryFn: () => getCategories({ search, isActive: activeFilter === '' ? undefined : activeFilter === 'true', page, pageSize: 10 }),
  })
  const saveMutation = useMutation({
    mutationFn: async (values: { name: string; description: string; isActive: boolean }) =>
      editing ? updateCategory(editing.categoryId, values) : createCategory(values),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
      setSuccessMessage(editing ? 'Categoría actualizada.' : 'Categoría creada.')
      setFormOpen(false)
      setEditing(null)
    },
  })
  const deactivateMutation = useMutation({
    mutationFn: deactivateCategory,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
      setSuccessMessage('Categoría desactivada.')
    },
  })

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    saveMutation.mutate({
      name: String(values.get('name') ?? '').trim(),
      description: String(values.get('description') ?? '').trim(),
      isActive: values.get('isActive') === 'true',
    })
  }

  function openNewForm() {
    setEditing(null)
    setFormOpen(true)
    setSuccessMessage('')
    saveMutation.reset()
  }

  return (
    <section className="catalog-page">
      <div className="page-heading">
        <div>
          <p className="eyebrow">Catálogo</p>
          <h1>Categorías</h1>
          <p className="page-description">Organiza los productos y conserva las categorías que ya tienen historial.</p>
        </div>
        <button className="primary-button" type="button" onClick={openNewForm}>Nueva categoría</button>
      </div>
      {successMessage && <p className="form-success" role="status">{successMessage}</p>}

      {formOpen && (
        <form className="catalog-form" key={editing?.categoryId ?? 'new'} onSubmit={submit}>
          <div className="form-heading"><h2>{editing ? 'Editar categoría' : 'Crear categoría'}</h2><button className="text-button" type="button" onClick={() => setFormOpen(false)}>Cerrar</button></div>
          <div className="form-grid">
            <label>Nombre<input name="name" required minLength={2} maxLength={80} defaultValue={editing?.name ?? ''} /></label>
            <label>Estado<select name="isActive" defaultValue={String(editing?.isActive ?? true)}><option value="true">Activa</option><option value="false">Inactiva</option></select></label>
            <label className="form-wide">Descripción<input name="description" maxLength={250} defaultValue={editing?.description ?? ''} /></label>
          </div>
          {saveMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(saveMutation.error)}</p>}
          <button className="primary-button" type="submit" disabled={saveMutation.isPending}>{saveMutation.isPending ? 'Guardando…' : 'Guardar categoría'}</button>
        </form>
      )}

      <div className="catalog-toolbar">
        <label className="toolbar-search">Buscar<input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="Nombre de categoría" /></label>
        <label>Estado<select value={activeFilter} onChange={(event) => { setActiveFilter(event.target.value); setPage(1) }}><option value="true">Activas</option><option value="false">Inactivas</option><option value="">Todas</option></select></label>
      </div>

      {query.isPending ? <p className="catalog-message">Cargando categorías…</p>
        : query.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(query.error)}</p>
          : query.data.items.length === 0 ? <p className="catalog-message">No hay categorías que coincidan con la búsqueda.</p>
            : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Nombre</th><th>Descripción</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>
              {query.data.items.map((category) => <tr key={category.categoryId}>
                <td>{category.name}</td><td>{category.description || '—'}</td><td><span className={`status-pill ${category.isActive ? 'active' : 'inactive'}`}>{category.isActive ? 'Activa' : 'Inactiva'}</span></td>
                <td className="row-actions"><button className="text-button" type="button" onClick={() => { setEditing(category); setFormOpen(true); setSuccessMessage(''); saveMutation.reset() }}>Editar</button>{category.isActive && <button className="text-button danger-text" type="button" disabled={deactivateMutation.isPending} onClick={() => deactivateMutation.mutate(category.categoryId)}>Desactivar</button>}</td>
              </tr>)}
            </tbody></table></div>}
      {deactivateMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(deactivateMutation.error)}</p>}
      {query.data && <div className="pagination"><span>{query.data.totalCount} categorías · página {query.data.page} de {Math.max(query.data.totalPages, 1)}</span><div><button className="secondary-button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button><button className="secondary-button" disabled={page >= query.data.totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button></div></div>}
    </section>
  )
}
