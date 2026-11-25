import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getProducts } from '../api/catalog'
import { createSale, getSale, getSales, type PaymentMethod, type SaleDetail, type SaleWrite } from '../api/sales'
import { getApiErrorMessage } from '../api/httpClient'
import { useAuth } from '../auth/AuthContext'

const paymentMethods: { value: PaymentMethod; label: string }[] = [
  { value: 0, label: 'Efectivo' },
  { value: 1, label: 'Tarjeta' },
  { value: 2, label: 'Transferencia' },
]

interface CartLine {
  productId: number
  sku: string
  name: string
  currentStock: number
  quantity: number
}

export function SalesPage() {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const canFilterByEmployee = user?.roles.includes('Administrador') ?? false
  const [productSearch, setProductSearch] = useState('')
  const [cart, setCart] = useState<CartLine[]>([])
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(0)
  const [receipt, setReceipt] = useState<SaleDetail | null>(null)
  const [selectedSaleId, setSelectedSaleId] = useState<number | null>(null)
  const [saleSearch, setSaleSearch] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [employeeId, setEmployeeId] = useState('')
  const [page, setPage] = useState(1)

  const productsQuery = useQuery({
    queryKey: ['sales', 'product-search', productSearch],
    queryFn: () => getProducts({ search: productSearch.trim(), isActive: true, page: 1, pageSize: 20 }),
    enabled: productSearch.trim().length >= 2,
  })
  const salesQuery = useQuery({
    queryKey: ['sales', saleSearch, fromDate, toDate, employeeId, page],
    queryFn: () => getSales({ search: saleSearch, fromDate: fromDate || undefined, toDate: toDate || undefined, createdByUserId: canFilterByEmployee ? employeeId || undefined : undefined, page, pageSize: 10 }),
  })
  const detailQuery = useQuery({
    queryKey: ['sales', 'detail', selectedSaleId],
    queryFn: () => getSale(selectedSaleId!),
    enabled: selectedSaleId !== null,
  })
  const createMutation = useMutation({
    mutationFn: (request: SaleWrite) => createSale(request),
    onSuccess: async (createdSale) => {
      setReceipt(createdSale)
      setSelectedSaleId(createdSale.saleId)
      setCart([])
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['sales'] }),
        queryClient.invalidateQueries({ queryKey: ['products'] }),
      ])
    },
  })

  function addProduct(product: NonNullable<typeof productsQuery.data>['items'][number]) {
    if (product.currentStock <= 0) return
    setCart((current) => {
      const existing = current.find((line) => line.productId === product.productId)
      if (existing) {
        return current.map((line) => line.productId === product.productId
          ? { ...line, currentStock: product.currentStock, quantity: Math.min(line.quantity + 1, product.currentStock) }
          : line)
      }
      return [...current, {
        productId: product.productId,
        sku: product.sku,
        name: product.name,
        currentStock: product.currentStock,
        quantity: 1,
      }]
    })
  }

  function submitSale(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (cart.length === 0) return
    createMutation.mutate({
      paymentMethod,
      items: cart.map(({ productId, quantity }) => ({ productId, quantity })),
    })
  }

  return (
    <section className="catalog-page sales-page">
      <div className="page-heading">
        <div><p className="eyebrow">Operaciones</p><h1>Ventas</h1><p className="page-description">Busca productos y confirma el cobro. El servidor valida existencias y calcula el comprobante.</p></div>
      </div>

      <div className="sales-layout">
        <section className="sales-card" aria-labelledby="cashier-title">
          <div className="section-heading-row"><h2 className="section-title" id="cashier-title">Caja</h2></div>
          <label className="sales-search">Buscar producto por nombre o SKU<input value={productSearch} onChange={(event) => setProductSearch(event.target.value)} placeholder="Ej. Agua o BEB-001" /></label>
          {productSearch.trim().length < 2 ? <p className="catalog-message">Escribe al menos dos caracteres para buscar productos activos.</p>
            : productsQuery.isPending ? <p className="catalog-message">Buscando productos…</p>
              : productsQuery.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(productsQuery.error)}</p>
                : productsQuery.data.items.length === 0 ? <p className="catalog-message">No se encontraron productos activos.</p>
                  : <div className="sales-product-results">{productsQuery.data.items.map((product) => <article className="sales-product" key={product.productId}>
                    <div><strong>{product.name}</strong><small>SKU {product.sku} · Stock {product.currentStock} · RD$ {product.salePrice.toFixed(2)}</small></div>
                    <button className="secondary-button" type="button" disabled={product.currentStock === 0} onClick={() => addProduct(product)}>Agregar</button>
                  </article>)}</div>}
        </section>

        <form className="sales-card sales-cart" onSubmit={submitSale}>
          <div className="section-heading-row"><h2 className="section-title">Carrito</h2><span>{cart.length} productos</span></div>
          {cart.length === 0 ? <p className="catalog-message">Agrega productos para iniciar una venta.</p>
            : <div className="sales-cart-lines">{cart.map((line) => <div className="sales-cart-line" key={line.productId}>
              <div><strong>{line.name}</strong><small>SKU {line.sku} · Stock disponible {line.currentStock}</small></div>
              <label>Cantidad<input type="number" min="1" max={line.currentStock} value={line.quantity} onChange={(event) => {
                const nextQuantity = Number(event.target.value)
                if (Number.isInteger(nextQuantity) && nextQuantity > 0) {
                  setCart((current) => current.map((item) => item.productId === line.productId
                    ? { ...item, quantity: Math.min(nextQuantity, item.currentStock) }
                    : item))
                }
              }} /></label>
              <button className="text-button danger-text" type="button" onClick={() => setCart((current) => current.filter((item) => item.productId !== line.productId))}>Quitar</button>
            </div>)}</div>}
          <label className="sales-payment">Método de pago<select value={paymentMethod} onChange={(event) => setPaymentMethod(Number(event.target.value) as PaymentMethod)}>
            {paymentMethods.map((method) => <option value={method.value} key={method.value}>{method.label}</option>)}
          </select></label>
          {createMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(createMutation.error)}</p>}
          <p className="form-hint">El total final se calcula y confirma en el servidor al registrar la venta.</p>
          <button className="primary-button" type="submit" disabled={cart.length === 0 || createMutation.isPending}>
            {createMutation.isPending ? 'Procesando…' : 'Confirmar venta'}
          </button>
        </form>
      </div>

      {receipt && <Receipt sale={receipt} onPrint={() => window.print()} />}

      <section className="sales-card sales-history">
        <div className="section-heading-row"><h2 className="section-title">Historial de ventas</h2></div>
        <div className="catalog-toolbar">
          <label className="toolbar-search">Número de venta<input value={saleSearch} onChange={(event) => { setSaleSearch(event.target.value); setPage(1) }} placeholder="SAL-…" /></label>
          <label>Desde<input type="date" value={fromDate} onChange={(event) => { setFromDate(event.target.value); setPage(1) }} /></label>
          <label>Hasta<input type="date" value={toDate} onChange={(event) => { setToDate(event.target.value); setPage(1) }} /></label>
          {canFilterByEmployee && <label>Id de empleado<input value={employeeId} onChange={(event) => { setEmployeeId(event.target.value); setPage(1) }} placeholder="Copia un ID del historial" /></label>}
        </div>
        {salesQuery.isPending ? <p className="catalog-message">Cargando ventas…</p>
          : salesQuery.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(salesQuery.error)}</p>
            : salesQuery.data.items.length === 0 ? <p className="catalog-message">No hay ventas que coincidan con los filtros.</p>
              : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Número</th><th>Fecha</th>{canFilterByEmployee && <th>Id empleado</th>}<th>Pago</th><th>Total</th><th /></tr></thead><tbody>
                {salesQuery.data.items.map((sale) => <tr key={sale.saleId}>
                  <td><strong>{sale.saleNumber}</strong><small>{sale.itemCount} líneas</small></td>
                  <td>{new Date(sale.createdAtUtc).toLocaleString()}</td>
                  {canFilterByEmployee && <td>{sale.createdByUserId}</td>}
                  <td>{paymentMethods.find((method) => method.value === sale.paymentMethod)?.label ?? '—'}</td>
                  <td>RD$ {sale.total.toFixed(2)}</td>
                  <td><button className="text-button" type="button" onClick={() => { setSelectedSaleId(sale.saleId); setReceipt(null) }}>Ver comprobante</button></td>
                </tr>)}
              </tbody></table></div>}
        {salesQuery.data && <div className="pagination"><span>{salesQuery.data.totalCount} ventas · página {salesQuery.data.page} de {Math.max(salesQuery.data.totalPages, 1)}</span><div>
          <button className="secondary-button" type="button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button>
          <button className="secondary-button" type="button" disabled={page >= salesQuery.data.totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button>
        </div></div>}
      </section>

      {selectedSaleId !== null && !receipt && (detailQuery.isPending ? <p className="catalog-message">Cargando comprobante…</p>
        : detailQuery.isError ? <p className="form-error" role="alert">{getApiErrorMessage(detailQuery.error)}</p>
          : detailQuery.data && <Receipt sale={detailQuery.data} onPrint={() => window.print()} />)}
    </section>
  )
}

function Receipt({ sale, onPrint }: { sale: SaleDetail; onPrint: () => void }) {
  return (
    <section className="sales-receipt" aria-labelledby="receipt-title">
      <div className="section-heading-row receipt-heading"><div><p className="eyebrow">Comprobante interno · no fiscal</p><h2 className="section-title" id="receipt-title">{sale.saleNumber}</h2></div><button className="secondary-button no-print" type="button" onClick={onPrint}>Imprimir</button></div>
      <p className="form-hint">{new Date(sale.createdAtUtc).toLocaleString()} · {paymentMethods.find((method) => method.value === sale.paymentMethod)?.label ?? '—'}</p>
      <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Producto</th><th>Cantidad</th><th>Precio</th><th>Subtotal</th></tr></thead><tbody>
        {sale.items.map((line) => <tr key={line.saleItemId}><td><strong>{line.productName}</strong><small>SKU {line.sku}</small></td><td>{line.quantity}</td><td>RD$ {line.unitPrice.toFixed(2)}</td><td>RD$ {line.lineTotal.toFixed(2)}</td></tr>)}
      </tbody></table></div>
      <div className="receipt-totals"><p>Subtotal <strong>RD$ {sale.subtotal.toFixed(2)}</strong></p><p>Descuento <strong>RD$ {sale.discount.toFixed(2)}</strong></p><p>Total <strong>RD$ {sale.total.toFixed(2)}</strong></p></div>
    </section>
  )
}
