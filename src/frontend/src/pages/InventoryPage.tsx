import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adjustStock, getLowStock, getMovements, getStock, type Product } from '../api/catalog'
import { getApiErrorMessage } from '../api/httpClient'

export function InventoryPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [lowStockPage, setLowStockPage] = useState(1)
  const [adjustmentProductId, setAdjustmentProductId] = useState('')
  const [movementProductId, setMovementProductId] = useState('')
  const [movementPage, setMovementPage] = useState(1)
  const stockQuery = useQuery({
    queryKey: ['inventory', 'stock', search, page],
    queryFn: () => getStock({ search, isActive: true, page, pageSize: 10 }),
  })
  const lowStockQuery = useQuery({
    queryKey: ['inventory', 'low-stock', search, lowStockPage],
    queryFn: () => getLowStock({ search, page: lowStockPage, pageSize: 5 }),
  })
  const movementsQuery = useQuery({
    queryKey: ['inventory', 'movements', movementProductId, movementPage],
    queryFn: () => getMovements({ productId: movementProductId ? Number(movementProductId) : undefined, page: movementPage, pageSize: 10 }),
  })
  const adjustmentMutation = useMutation({
    mutationFn: adjustStock,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['inventory'] }),
        queryClient.invalidateQueries({ queryKey: ['products'] }),
      ])
    },
  })

  function submitAdjustment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    adjustmentMutation.mutate({
      productId: Number(values.get('productId')),
      quantityDelta: Number(values.get('quantityDelta')),
      reason: String(values.get('reason') ?? '').trim(),
    })
    event.currentTarget.reset()
  }

  return (
    <section className="catalog-page">
      <div className="page-heading"><div><p className="eyebrow">Operaciones</p><h1>Inventario</h1><p className="page-description">Consulta existencias, revisa alertas y registra ajustes con motivo.</p></div></div>

      <div className="inventory-alert">
        <div><p className="eyebrow">Alerta de existencias</p><h2>En el mínimo o por debajo</h2><p>Productos activos con stock igual o menor al mínimo configurado.</p></div>
        {lowStockQuery.isPending ? <span>Cargando…</span>
          : lowStockQuery.isError ? <span className="error-text">{getApiErrorMessage(lowStockQuery.error)}</span>
            : <strong className="alert-count">{lowStockQuery.data.totalCount}</strong>}
        {lowStockQuery.data && lowStockQuery.data.items.length > 0 && <>
          <ul>{lowStockQuery.data.items.map((product) => <li key={product.productId}><span>{product.name} <small>({product.sku})</small></span><strong>{product.currentStock} / mín. {product.minimumStock}</strong></li>)}</ul>
          <div className="low-stock-pagination"><span>Página {lowStockQuery.data.page} de {Math.max(lowStockQuery.data.totalPages, 1)}</span><div><button className="secondary-button" disabled={lowStockPage <= 1} onClick={() => setLowStockPage((value) => value - 1)}>Anterior</button><button className="secondary-button" disabled={lowStockPage >= lowStockQuery.data.totalPages} onClick={() => setLowStockPage((value) => value + 1)}>Siguiente</button></div></div>
        </>}
      </div>

      <form className="catalog-form adjustment-form" onSubmit={submitAdjustment}>
        <div className="form-heading"><div><h2>Ajustar existencias</h2><p className="form-hint">Los cambios quedan registrados en movimientos y auditoría.</p></div></div>
        <div className="form-grid">
          <label>Producto<select name="productId" required value={adjustmentProductId} onChange={(event) => setAdjustmentProductId(event.target.value)}><option value="" disabled>Selecciona en la lista de stock</option>{stockQuery.data?.items.map((product) => <option key={product.productId} value={product.productId}>{product.name} · {product.sku} · {product.currentStock} unidades</option>)}</select></label>
          <label>Cantidad a sumar o restar<input name="quantityDelta" type="number" step="1" required placeholder="Ej.: 5 o -2" /></label>
          <label className="form-wide">Motivo<input name="reason" required minLength={3} maxLength={250} placeholder="Conteo físico, merma, corrección…" /></label>
        </div>
        {adjustmentMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(adjustmentMutation.error)}</p>}
        {adjustmentMutation.isSuccess && <p className="form-success" role="status">Ajuste guardado. Stock actualizado a {adjustmentMutation.data.product.currentStock} unidades.</p>}
        <button className="primary-button" type="submit" disabled={adjustmentMutation.isPending || stockQuery.isPending || !stockQuery.data?.items.length}>{adjustmentMutation.isPending ? 'Guardando…' : 'Registrar ajuste'}</button>
      </form>

      <div className="catalog-toolbar">
        <label className="toolbar-search">Buscar producto<input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); setLowStockPage(1) }} placeholder="Nombre o SKU" /></label>
      </div>
      <h2 className="section-title">Existencias</h2>
      {stockQuery.isPending ? <p className="catalog-message">Cargando existencias…</p>
        : stockQuery.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(stockQuery.error)}</p>
          : stockQuery.data.items.length === 0 ? <p className="catalog-message">No hay productos activos para mostrar.</p>
            : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Producto</th><th>Categoría</th><th>Stock actual</th><th>Stock mínimo</th><th>Estado</th></tr></thead><tbody>
              {stockQuery.data.items.map((product: Product) => <tr key={product.productId}><td><strong>{product.name}</strong><small>SKU {product.sku}</small></td><td>{product.categoryName}</td><td className={product.currentStock <= product.minimumStock ? 'stock-low' : ''}>{product.currentStock}</td><td>{product.minimumStock}</td><td>{product.currentStock <= product.minimumStock ? <span className="status-pill warning">Reponer</span> : <span className="status-pill active">Disponible</span>}</td></tr>)}
            </tbody></table></div>}
      {stockQuery.data && <div className="pagination"><span>{stockQuery.data.totalCount} productos · página {stockQuery.data.page} de {Math.max(stockQuery.data.totalPages, 1)}</span><div><button className="secondary-button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button><button className="secondary-button" disabled={page >= stockQuery.data.totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button></div></div>}

      <div className="section-heading-row"><h2 className="section-title">Movimientos recientes</h2><label>Producto<select value={movementProductId} onChange={(event) => { setMovementProductId(event.target.value); setMovementPage(1) }}><option value="">Todos</option>{stockQuery.data?.items.map((product) => <option key={product.productId} value={product.productId}>{product.name}</option>)}</select></label></div>
      {movementsQuery.isPending ? <p className="catalog-message">Cargando movimientos…</p>
        : movementsQuery.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(movementsQuery.error)}</p>
          : movementsQuery.data.items.length === 0 ? <p className="catalog-message">Todavía no hay movimientos para mostrar.</p>
            : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Fecha</th><th>Producto</th><th>Tipo</th><th>Variación</th><th>Balance</th><th>Motivo</th></tr></thead><tbody>
              {movementsQuery.data.items.map((movement) => <tr key={movement.stockMovementId}><td>{new Date(movement.createdAtUtc).toLocaleString()}</td><td><strong>{movement.productName}</strong><small>SKU {movement.sku}</small></td><td>{['Compra', 'Venta', 'Ajuste'][movement.type] ?? 'Movimiento'}</td><td className={movement.quantityDelta < 0 ? 'danger-text' : 'positive-text'}>{movement.quantityDelta > 0 ? '+' : ''}{movement.quantityDelta}</td><td>{movement.stockBefore} → {movement.stockAfter}</td><td>{movement.notes || '—'}</td></tr>)}
            </tbody></table></div>}
      {movementsQuery.data && <div className="pagination"><span>{movementsQuery.data.totalCount} movimientos · página {movementsQuery.data.page} de {Math.max(movementsQuery.data.totalPages, 1)}</span><div><button className="secondary-button" disabled={movementPage <= 1} onClick={() => setMovementPage((value) => value - 1)}>Anterior</button><button className="secondary-button" disabled={movementPage >= movementsQuery.data.totalPages} onClick={() => setMovementPage((value) => value + 1)}>Siguiente</button></div></div>}
    </section>
  )
}
