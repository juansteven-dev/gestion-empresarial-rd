import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createProduct, deactivateProduct, getCategories, getProducts, updateProduct, type Product, type ProductWrite } from '../api/catalog'
import { getApiErrorMessage } from '../api/httpClient'
import { hasCatalogManagementRole } from '../auth/RequireRoles'
import { useAuth } from '../auth/AuthContext'

export function ProductsPage() {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const canManage = hasCatalogManagementRole(user?.roles)
  const [search, setSearch] = useState('')
  const [activeFilter, setActiveFilter] = useState('true')
  const [categoryId, setCategoryId] = useState('')
  const [page, setPage] = useState(1)
  const [editing, setEditing] = useState<Product | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const query = useQuery({
    queryKey: ['products', search, activeFilter, categoryId, page],
    queryFn: () => getProducts({ search, isActive: activeFilter === '' ? undefined : activeFilter === 'true', categoryId: categoryId ? Number(categoryId) : undefined, page, pageSize: 10 }),
  })
  const categoriesQuery = useQuery({
    queryKey: ['categories', 'active-for-products'],
    queryFn: () => getCategories({ pageSize: 100 }),
  })
  const saveMutation = useMutation({
    mutationFn: async (request: ProductWrite) => editing ? updateProduct(editing.productId, request) : createProduct(request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['products'] }),
        queryClient.invalidateQueries({ queryKey: ['inventory'] }),
      ])
      setFormOpen(false)
      setEditing(null)
    },
  })
  const deactivateMutation = useMutation({
    mutationFn: deactivateProduct,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['products'] })
      await queryClient.invalidateQueries({ queryKey: ['inventory'] })
    },
  })

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    saveMutation.mutate({
      sku: String(values.get('sku') ?? '').trim(),
      name: String(values.get('name') ?? '').trim(),
      description: String(values.get('description') ?? '').trim(),
      categoryId: Number(values.get('categoryId')),
      costPrice: Number(values.get('costPrice')),
      salePrice: Number(values.get('salePrice')),
      minimumStock: Number(values.get('minimumStock')),
      isActive: values.get('isActive') === 'true',
    })
  }

  function openNewForm() {
    setEditing(null)
    setFormOpen(true)
    saveMutation.reset()
  }

  return (
    <section className="catalog-page">
      <div className="page-heading">
        <div><p className="eyebrow">Catálogo</p><h1>Productos</h1><p className="page-description">Busca productos por nombre o SKU y consulta sus precios y existencias.</p></div>
        {canManage && <button className="primary-button" type="button" onClick={openNewForm}>Nuevo producto</button>}
      </div>

      {formOpen && canManage && <form className="catalog-form" key={editing?.productId ?? 'new'} onSubmit={submit}>
        <div className="form-heading"><h2>{editing ? 'Editar producto' : 'Crear producto'}</h2><button className="text-button" type="button" onClick={() => setFormOpen(false)}>Cerrar</button></div>
        {categoriesQuery.isError && <p className="form-error" role="alert">{getApiErrorMessage(categoriesQuery.error)}</p>}
        <div className="form-grid">
          <label>SKU<input name="sku" required maxLength={40} defaultValue={editing?.sku ?? ''} /></label>
          <label>Nombre<input name="name" required minLength={2} maxLength={140} defaultValue={editing?.name ?? ''} /></label>
          <label>Categoría<select name="categoryId" required defaultValue={String(editing?.categoryId ?? '')}><option value="" disabled>Selecciona una categoría</option>{categoriesQuery.data?.items.filter((item) => item.isActive || item.categoryId === editing?.categoryId).map((item) => <option key={item.categoryId} value={item.categoryId}>{item.name}</option>)}</select></label>
          <label>Precio de costo<input name="costPrice" type="number" min="0" step="0.01" required defaultValue={editing?.costPrice ?? 0} /></label>
          <label>Precio de venta<input name="salePrice" type="number" min="0" step="0.01" required defaultValue={editing?.salePrice ?? 0} /></label>
          <label>Existencia mínima<input name="minimumStock" type="number" min="0" step="1" required defaultValue={editing?.minimumStock ?? 0} /></label>
          <label>Estado<select name="isActive" defaultValue={String(editing?.isActive ?? true)}><option value="true">Activo</option><option value="false">Inactivo</option></select></label>
          <label className="form-wide">Descripción<input name="description" maxLength={500} defaultValue={editing?.description ?? ''} /></label>
        </div>
        <p className="form-hint">La existencia solo cambia mediante un ajuste de inventario.</p>
        {saveMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(saveMutation.error)}</p>}
        <button className="primary-button" type="submit" disabled={saveMutation.isPending || categoriesQuery.isPending}>{saveMutation.isPending ? 'Guardando…' : 'Guardar producto'}</button>
      </form>}

      <div className="catalog-toolbar">
        <label className="toolbar-search">Buscar<input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="Nombre o SKU" /></label>
        <label>Estado<select value={activeFilter} onChange={(event) => { setActiveFilter(event.target.value); setPage(1) }}><option value="true">Activos</option><option value="false">Inactivos</option><option value="">Todos</option></select></label>
        <label>Categoría<select value={categoryId} onChange={(event) => { setCategoryId(event.target.value); setPage(1) }}><option value="">Todas</option>{categoriesQuery.data?.items.map((item) => <option key={item.categoryId} value={item.categoryId}>{item.name}</option>)}</select></label>
      </div>

      {query.isPending ? <p className="catalog-message">Cargando productos…</p>
        : query.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(query.error)}</p>
          : query.data.items.length === 0 ? <p className="catalog-message">No hay productos que coincidan con la búsqueda.</p>
            : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Producto</th><th>Categoría</th><th>Precio venta</th><th>Existencia</th><th>Estado</th>{canManage && <th>Acciones</th>}</tr></thead><tbody>
              {query.data.items.map((product) => <tr key={product.productId}>
                <td><strong>{product.name}</strong><small>SKU {product.sku}</small></td><td>{product.categoryName}</td><td>RD$ {product.salePrice.toFixed(2)}</td><td><span className={product.currentStock <= product.minimumStock ? 'stock-low' : ''}>{product.currentStock}</span><small>Mínimo {product.minimumStock}</small></td><td><span className={`status-pill ${product.isActive ? 'active' : 'inactive'}`}>{product.isActive ? 'Activo' : 'Inactivo'}</span></td>
                {canManage && <td className="row-actions"><button className="text-button" type="button" onClick={() => { setEditing(product); setFormOpen(true); saveMutation.reset() }}>Editar</button>{product.isActive && <button className="text-button danger-text" type="button" disabled={deactivateMutation.isPending} onClick={() => deactivateMutation.mutate(product.productId)}>Desactivar</button>}</td>}
              </tr>)}
            </tbody></table></div>}
      {deactivateMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(deactivateMutation.error)}</p>}
      {query.data && <div className="pagination"><span>{query.data.totalCount} productos · página {query.data.page} de {Math.max(query.data.totalPages, 1)}</span><div><button className="secondary-button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button><button className="secondary-button" disabled={page >= query.data.totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button></div></div>}
    </section>
  )
}
