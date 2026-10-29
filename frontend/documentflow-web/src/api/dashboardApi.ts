import { apiRequest } from './apiClient'
import type { DashboardHealth, DashboardSummary, OperationsSummary } from '../types/dashboard'

export async function getDashboard(): Promise<DashboardSummary> {
  return apiRequest<DashboardSummary>('/api/dashboard')
}

export function getDashboardSummary(signal?: AbortSignal): Promise<OperationsSummary> {
  return apiRequest<OperationsSummary>('/api/dashboard/summary', { signal })
}

export function getDashboardHealth(signal?: AbortSignal): Promise<DashboardHealth> {
  return apiRequest<DashboardHealth>('/api/dashboard/health', { signal })
}
