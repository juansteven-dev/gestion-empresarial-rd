import { apiClient } from './httpClient'
import type { Page } from './catalog'

export interface PeriodQuery {
  fromDate?: string
  toDate?: string
}

export interface DashboardSummary {
  fromDate: string
  toDate: string
  salesTotal: number | null
  saleCount: number | null
  lowStockCount: number | null
  lowStockProducts: { productId: number; sku: string; name: string; currentStock: number; minimumStock: number }[]
  topProducts: { productId: number; sku: string; name: string; quantity: number; total: number }[]
}

export interface ReportsSummary {
  fromDate: string
  toDate: string
  salesByDay: { dayUtc: string; saleCount: number; total: number }[]
  topProducts: { productId: number; sku: string; name: string; quantity: number; total: number }[]
  movements: { type: number; movementCount: number; quantityDelta: number }[]
}

export interface AuditLog {
  auditLogId: number
  userId: string
  userName: string
  action: string
  entityName: string
  entityId: string | null
  detailsJson: string | null
  ipAddress: string | null
  createdAtUtc: string
}

export interface UserDirectoryItem {
  id: string
  userName: string
  email: string | null
  roles: string[]
  isLockedOut: boolean
}

export interface CreatedUser {
  id: string
  userName: string
  email: string | null
  roles: string[]
}

export interface CreateUserRequest {
  userName: string
  email: string
  password: string
  role: string
}

function toQueryString(query: object): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  const serialized = params.toString()
  return serialized ? `?${serialized}` : ''
}

export async function getDashboard(query: PeriodQuery = {}) {
  const { data } = await apiClient.get<DashboardSummary>(`/dashboard${toQueryString(query)}`)
  return data
}

export async function getReports(query: PeriodQuery = {}) {
  const { data } = await apiClient.get<ReportsSummary>(`/reports${toQueryString(query)}`)
  return data
}

export async function getAuditLogs(query: PeriodQuery & { search?: string; userId?: string; page?: number; pageSize?: number }) {
  const { data } = await apiClient.get<Page<AuditLog>>(`/audit${toQueryString(query)}`)
  return data
}

export async function getUsers(query: { search?: string; page?: number; pageSize?: number }) {
  const { data } = await apiClient.get<Page<UserDirectoryItem>>(`/users${toQueryString(query)}`)
  return data
}

export async function createUser(request: CreateUserRequest) {
  const { data } = await apiClient.post<CreatedUser>('/users', request)
  return data
}
