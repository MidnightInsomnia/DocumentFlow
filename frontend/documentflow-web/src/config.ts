const configuredUrl = import.meta.env.VITE_API_BASE_URL as string | undefined

export const API_BASE_URL = (configuredUrl ?? 'http://localhost:5214').replace(/\/$/, '')
