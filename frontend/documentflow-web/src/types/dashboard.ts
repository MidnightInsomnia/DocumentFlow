export interface DashboardStatusCounts {
  pendingProcessing: number
  processing: number
  needsReview: number
  completed: number
  failed: number
}
export interface DashboardTypeCounts {
  unknown: number
  invoice: number
  contract: number
  act: number
}
export interface DashboardDepartmentCounts {
  unassigned: number
  accounting: number
  legal: number
}
export interface DailyDocumentCount { date: string; count: number }
// Legacy dashboard contract remains available for other callers.
export interface DashboardSummary {
  totalDocuments: number
  archivedDocuments: number
  statuses: DashboardStatusCounts
  types: DashboardTypeCounts
  departments: DashboardDepartmentCounts
  automaticallyCompleted: number
  manuallyCompleted: number
  automationRate: number
  manualReviewRate: number
  failureRate: number
  documentsLast7Days: number
  dailyDocuments: DailyDocumentCount[]
}

export interface OperationsSummary {
  pendingProcessing: number
  processing: number
  needsReview: number
  failed: number
  completedToday: number
  totalActive: number
  archivedTotal: number
  recentNeedsReview: { id: string; fileName: string; type: string; createdAtUtc: string }[]
  recentFailed: { id: string; fileName: string; createdAtUtc: string; reason: string }[]
}
export interface DashboardHealth {
  overall: 'Healthy' | 'Critical' | 'Degraded' | 'Unknown'
  api?: 'Healthy'
  postgresql?: 'Healthy' | 'Unavailable' | 'Unknown'
  worker?: { status: 'Healthy' | 'Stale' | 'Unknown' | 'Unavailable'; healthyInstances: number; staleInstances: number }
  ocr?: { status: 'Healthy' | 'Unavailable' | 'Unknown'; languages: string[] }
  ai?: { status: 'Disabled' | 'Available' | 'Unavailable' | 'ModelUnavailable' }
  version?: string
  runtime?: string
}
