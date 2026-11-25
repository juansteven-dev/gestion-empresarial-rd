import { apiClient } from './httpClient'
import type { Page } from './catalog'

export interface PurchaseSummary {
  purchaseId: number
  purchaseNumber: string
  supplierId: number
  supplierName: string
  status: number
  subtotal: number
  total: number
  itemCount: number
  createdAtUtc: string
  receivedAtUtc: string | null
}

export interface PurchaseLine {
  purchaseItemId: number
  productId: number
  sku: string
  productName: string
  quantity: number
  unitCost: number
  lineTotal: number
}

export interface PurchaseDetail extends PurchaseSummary {
  createdByUserId: string
  items: PurchaseLine[]
}

export interface PurchaseQuery {
  search?: string
  supplierId?: number
  status?: string
  page?: number
  pageSize?: number
}

export interface PurchaseWrite {
  supplierId: number
  items: { productId: number; quantity: number; unitCost: number }[]
}

function toQueryString(query: PurchaseQuery): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  const serialized = params.toString()
  return serialized ? `?${serialized}` : ''
}

export async function getPurchases(query: PurchaseQuery) {
  const { data } = await apiClient.get<Page<PurchaseSummary>>(`/purchases${toQueryString(query)}`)
  return data
}

export async function getPurchase(id: number) {
  const { data } = await apiClient.get<PurchaseDetail>(`/purchases/${id}`)
  return data
}

export async function createPurchase(request: PurchaseWrite) {
  const { data } = await apiClient.post<PurchaseDetail>('/purchases', request)
  return data
}

export async function receivePurchase(id: number) {
  const { data } = await apiClient.post<PurchaseDetail>(`/purchases/${id}/receive`)
  return data
}
