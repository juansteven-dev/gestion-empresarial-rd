import { apiClient } from './httpClient'
import type { Page } from './catalog'

export type PaymentMethod = 0 | 1 | 2

export interface SaleSummary {
  saleId: number
  saleNumber: string
  status: number
  subtotal: number
  discount: number
  total: number
  paymentMethod: PaymentMethod
  itemCount: number
  createdByUserId: string
  createdAtUtc: string
}

export interface SaleLine {
  saleItemId: number
  productId: number
  sku: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface SaleDetail extends SaleSummary {
  items: SaleLine[]
}

export interface SaleQuery {
  search?: string
  fromDate?: string
  toDate?: string
  createdByUserId?: string
  page?: number
  pageSize?: number
}

export interface SaleWrite {
  paymentMethod: PaymentMethod
  items: { productId: number; quantity: number }[]
}

function toQueryString(query: SaleQuery): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  const serialized = params.toString()
  return serialized ? `?${serialized}` : ''
}

export async function getSales(query: SaleQuery) {
  const { data } = await apiClient.get<Page<SaleSummary>>(`/sales${toQueryString(query)}`)
  return data
}

export async function getSale(id: number) {
  const { data } = await apiClient.get<SaleDetail>(`/sales/${id}`)
  return data
}

export async function createSale(request: SaleWrite) {
  const { data } = await apiClient.post<SaleDetail>('/sales', request)
  return data
}
