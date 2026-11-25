import { apiClient } from './httpClient'

export interface Page<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface Category {
  categoryId: number
  name: string
  description: string | null
  isActive: boolean
}

export interface CategoryWrite {
  name: string
  description: string
  isActive: boolean
}

export interface Product {
  productId: number
  sku: string
  name: string
  description: string | null
  categoryId: number
  categoryName: string
  costPrice: number
  salePrice: number
  currentStock: number
  minimumStock: number
  isActive: boolean
  updatedAtUtc: string
}

export interface ProductWrite {
  sku: string
  name: string
  description: string
  categoryId: number
  costPrice: number
  salePrice: number
  minimumStock: number
  isActive: boolean
}

export interface Supplier {
  supplierId: number
  name: string
  taxId: string | null
  phone: string | null
  email: string | null
  address: string | null
  isActive: boolean
}

export interface SupplierWrite {
  name: string
  taxId: string
  phone: string
  email: string
  address: string
  isActive: boolean
}

export interface StockMovement {
  stockMovementId: number
  productId: number
  sku: string
  productName: string
  type: number
  quantityDelta: number
  stockBefore: number
  stockAfter: number
  notes: string | null
  createdByUserId: string
  createdAtUtc: string
}

export interface StockAdjustmentResult {
  product: Product
  movement: StockMovement
}

export interface ListQuery {
  search?: string
  isActive?: boolean
  page?: number
  pageSize?: number
  categoryId?: number
  lowStockOnly?: boolean
}

export interface MovementQuery {
  productId?: number
  page?: number
  pageSize?: number
}

function toQueryString(query: object): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  const serialized = params.toString()
  return serialized ? `?${serialized}` : ''
}

export async function getCategories(query: ListQuery = {}) {
  const { data } = await apiClient.get<Page<Category>>(`/categories${toQueryString(query)}`)
  return data
}

export async function createCategory(request: CategoryWrite) {
  const { data } = await apiClient.post<Category>('/categories', request)
  return data
}

export async function updateCategory(id: number, request: CategoryWrite) {
  const { data } = await apiClient.put<Category>(`/categories/${id}`, request)
  return data
}

export async function deactivateCategory(id: number) {
  await apiClient.delete(`/categories/${id}`)
}

export async function getProducts(query: ListQuery = {}) {
  const { data } = await apiClient.get<Page<Product>>(`/products${toQueryString(query)}`)
  return data
}

export async function createProduct(request: ProductWrite) {
  const { data } = await apiClient.post<Product>('/products', request)
  return data
}

export async function updateProduct(id: number, request: ProductWrite) {
  const { data } = await apiClient.put<Product>(`/products/${id}`, request)
  return data
}

export async function deactivateProduct(id: number) {
  await apiClient.delete(`/products/${id}`)
}

export async function getSuppliers(query: ListQuery = {}) {
  const { data } = await apiClient.get<Page<Supplier>>(`/suppliers${toQueryString(query)}`)
  return data
}

export async function createSupplier(request: SupplierWrite) {
  const { data } = await apiClient.post<Supplier>('/suppliers', request)
  return data
}

export async function updateSupplier(id: number, request: SupplierWrite) {
  const { data } = await apiClient.put<Supplier>(`/suppliers/${id}`, request)
  return data
}

export async function deactivateSupplier(id: number) {
  await apiClient.delete(`/suppliers/${id}`)
}

export async function getStock(query: ListQuery = {}) {
  const { data } = await apiClient.get<Page<Product>>(`/inventory/stock${toQueryString(query)}`)
  return data
}

export async function getLowStock(query: ListQuery = {}) {
  const { data } = await apiClient.get<Page<Product>>(`/inventory/low-stock${toQueryString(query)}`)
  return data
}

export async function getMovements(query: MovementQuery = {}) {
  const { data } = await apiClient.get<Page<StockMovement>>(`/inventory/movements${toQueryString(query)}`)
  return data
}

export async function adjustStock(request: { productId: number; quantityDelta: number; reason: string }) {
  const { data } = await apiClient.post<StockAdjustmentResult>('/inventory/adjustments', request)
  return data
}
