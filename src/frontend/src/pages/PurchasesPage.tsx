import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getProducts, getSuppliers } from '../api/catalog'
import { createPurchase, getPurchase, getPurchases, receivePurchase, type PurchaseWrite } from '../api/purchases'
import { getApiErrorMessage } from '../api/httpClient'

interface DraftLine {
  productId: string
  quantity: string
  unitCost: string
}

const purchaseStatusLabels: Record<number, string> = {
  0: 'Borrador',
  1: 'Recibida',
  2: 'Cancelada',
}

export function PurchasesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [formOpen, setFormOpen] = useState(false)
  const [selectedPurchaseId, setSelectedPurchaseId] = useState<number | null>(null)
  const [supplierId, setSupplierId] = useState('')
  const [lines, setLines] = useState<DraftLine[]>([{ productId: '', quantity: '1', unitCost: '0' }])

  const purchasesQuery = useQuery({
    queryKey: ['purchases', search, status, page],
    queryFn: () => getPurchases({ search, status: status || undefined, page, pageSize: 10 }),
  })
  const suppliersQuery = useQuery({
    queryKey: ['suppliers', 'active-for-purchases'],
    queryFn: () => getSuppliers({ isActive: true, pageSize: 100 }),
  })
  const productsQuery = useQuery({
    queryKey: ['products', 'active-for-purchases'],
    queryFn: () => getProducts({ isActive: true, pageSize: 100 }),
  })
  const detailQuery = useQuery({
    queryKey: ['purchase', selectedPurchaseId],
    queryFn: () => getPurchase(selectedPurchaseId!),
    enabled: selectedPurchaseId !== null,
  })

  const createMutation = useMutation({
    mutationFn: createPurchase,
    onSuccess: async (purchase) => {
      await queryClient.invalidateQueries({ queryKey: ['purchases'] })
      setFormOpen(false)
      setSupplierId('')
      setLines([{ productId: '', quantity: '1', unitCost: '0' }])
      setSelectedPurchaseId(purchase.purchaseId)
    },
  })
  const receiveMutation = useMutation({
    mutationFn: receivePurchase,
    onSuccess: async (purchase) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['purchases'] }),
        queryClient.invalidateQueries({ queryKey: ['purchase', purchase.purchaseId] }),
        queryClient.invalidateQueries({ queryKey: ['inventory'] }),
        queryClient.invalidateQueries({ queryKey: ['products'] }),
      ])
    },
  })

  function updateLine(index: number, key: keyof DraftLine, value: string) {
    setLines((current) => current.map((line, lineIndex) => {
      if (lineIndex !== index) return line
      if (key === 'productId') {
        const product = productsQuery.data?.items.find((item) => item.productId === Number(value))
        return { ...line, productId: value, unitCost: product ? String(product.costPrice) : line.unitCost }
      }
      return { ...line, [key]: value }
    }))
  }

  function submitDraft(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const request: PurchaseWrite = {
      supplierId: Number(supplierId),
      items: lines.map((line) => ({
        productId: Number(line.productId),
        quantity: Number(line.quantity),
        unitCost: Number(line.unitCost),
      })),
    }
    createMutation.mutate(request)
  }

  const estimatedTotal = lines.reduce(
    (total, line) => total + (Number(line.quantity) || 0) * (Number(line.unitCost) || 0),
    0,
  )

  return (
    <section className="catalog-page">
      <div className="page-heading">
        <div><p className="eyebrow">Abastecimiento</p><h1>Compras</h1><p className="page-description">Crea borradores y registra la recepción de mercancía para actualizar el inventario.</p></div>
        <button className="primary-button" type="button" onClick={() => { setFormOpen((open) => !open); createMutation.reset() }}>Nueva compra</button>
      </div>

      <div className="purchase-note"><strong>Un borrador no cambia existencias.</strong><span>El stock aumenta únicamente al recibir la compra.</span></div>

      {formOpen && <form className="catalog-form" onSubmit={submitDraft}>
        <div className="form-heading"><div><h2>Nuevo borrador</h2><p className="form-hint">Selecciona un suplidor activo y agrega al menos una línea.</p></div><button className="text-button" type="button" onClick={() => setFormOpen(false)}>Cerrar</button></div>
        <label className="purchase-supplier-field">Suplidor<select required value={supplierId} onChange={(event) => setSupplierId(event.target.value)}><option value="" disabled>Selecciona un suplidor</option>{suppliersQuery.data?.items.map((supplier) => <option key={supplier.supplierId} value={supplier.supplierId}>{supplier.name}</option>)}</select></label>
        {suppliersQuery.isError && <p className="form-error" role="alert">{getApiErrorMessage(suppliersQuery.error)}</p>}
        {productsQuery.isError && <p className="form-error" role="alert">{getApiErrorMessage(productsQuery.error)}</p>}
        {productsQuery.data?.items.length === 0 && <p className="catalog-message">No hay productos activos. Crea un producto antes de registrar una compra.</p>}
        <div className="purchase-lines">
          {lines.map((line, index) => <div className="purchase-line" key={index}>
            <label>Producto<select required value={line.productId} onChange={(event) => updateLine(index, 'productId', event.target.value)}><option value="" disabled>Selecciona producto</option>{productsQuery.data?.items.map((product) => <option key={product.productId} value={product.productId}>{product.name} · {product.sku}</option>)}</select></label>
            <label>Cantidad<input required type="number" min="1" step="1" value={line.quantity} onChange={(event) => updateLine(index, 'quantity', event.target.value)} /></label>
            <label>Costo unitario<input required type="number" min="0" step="0.01" value={line.unitCost} onChange={(event) => updateLine(index, 'unitCost', event.target.value)} /></label>
            <button className="text-button danger-text" type="button" aria-label={`Quitar línea ${index + 1}`} disabled={lines.length === 1} onClick={() => setLines((current) => current.filter((_, lineIndex) => lineIndex !== index))}>Quitar</button>
          </div>)}
        </div>
        <div className="purchase-form-footer"><button className="secondary-button" type="button" disabled={lines.length >= 100} onClick={() => setLines((current) => [...current, { productId: '', quantity: '1', unitCost: '0' }])}>Agregar línea</button><strong>Total estimado: RD$ {estimatedTotal.toFixed(2)}</strong></div>
        {createMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(createMutation.error)}</p>}
        <button className="primary-button" type="submit" disabled={createMutation.isPending || suppliersQuery.isPending || productsQuery.isPending || !suppliersQuery.data?.items.length || !productsQuery.data?.items.length}>{createMutation.isPending ? 'Guardando…' : 'Guardar borrador'}</button>
      </form>}

      <div className="catalog-toolbar">
        <label className="toolbar-search">Buscar<input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="Número de compra o suplidor" /></label>
        <label>Estado<select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}><option value="">Todos</option><option value="Draft">Borrador</option><option value="Received">Recibida</option><option value="Cancelled">Cancelada</option></select></label>
      </div>

      {purchasesQuery.isPending ? <p className="catalog-message">Cargando compras…</p>
        : purchasesQuery.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(purchasesQuery.error)}</p>
          : purchasesQuery.data.items.length === 0 ? <p className="catalog-message">No hay compras que coincidan con el filtro.</p>
            : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Número</th><th>Suplidor</th><th>Fecha</th><th>Líneas</th><th>Total</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>
              {purchasesQuery.data.items.map((purchase) => <tr key={purchase.purchaseId}>
                <td><strong>{purchase.purchaseNumber}</strong></td><td>{purchase.supplierName}</td><td>{new Date(purchase.createdAtUtc).toLocaleString()}</td><td>{purchase.itemCount}</td><td>RD$ {purchase.total.toFixed(2)}</td><td><span className={`status-pill ${purchase.status === 1 ? 'active' : 'inactive'}`}>{purchaseStatusLabels[purchase.status] ?? 'Desconocido'}</span></td>
                <td className="row-actions"><button className="text-button" type="button" onClick={() => setSelectedPurchaseId(purchase.purchaseId)}>Detalle</button>{purchase.status === 0 && <button className="text-button" type="button" disabled={receiveMutation.isPending} onClick={() => { receiveMutation.reset(); receiveMutation.mutate(purchase.purchaseId) }}>Recibir</button>}</td>
              </tr>)}
            </tbody></table></div>}
      {receiveMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(receiveMutation.error)}</p>}
      {purchasesQuery.data && <div className="pagination"><span>{purchasesQuery.data.totalCount} compras · página {purchasesQuery.data.page} de {Math.max(purchasesQuery.data.totalPages, 1)}</span><div><button className="secondary-button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button><button className="secondary-button" disabled={page >= purchasesQuery.data.totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button></div></div>}

      {selectedPurchaseId !== null && <div className="purchase-detail-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setSelectedPurchaseId(null) }}>
        <section className="purchase-detail-panel" role="dialog" aria-modal="true" aria-labelledby="purchase-detail-title">
          <div className="form-heading"><h2 id="purchase-detail-title">Detalle de compra</h2><button className="text-button" type="button" onClick={() => setSelectedPurchaseId(null)}>Cerrar</button></div>
          {detailQuery.isPending ? <p className="catalog-message">Cargando detalle…</p>
            : detailQuery.isError ? <p className="form-error" role="alert">{getApiErrorMessage(detailQuery.error)}</p>
              : detailQuery.data && <>
                <p><strong>{detailQuery.data.purchaseNumber}</strong> · {detailQuery.data.supplierName}</p>
                <p>Estado: {purchaseStatusLabels[detailQuery.data.status] ?? 'Desconocido'} · Total: RD$ {detailQuery.data.total.toFixed(2)}</p>
                <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Producto</th><th>Cantidad</th><th>Costo unitario</th><th>Total</th></tr></thead><tbody>{detailQuery.data.items.map((item) => <tr key={item.purchaseItemId}><td><strong>{item.productName}</strong><small>SKU {item.sku}</small></td><td>{item.quantity}</td><td>RD$ {item.unitCost.toFixed(2)}</td><td>RD$ {item.lineTotal.toFixed(2)}</td></tr>)}</tbody></table></div>
              </>}
        </section>
      </div>}
    </section>
  )
}
