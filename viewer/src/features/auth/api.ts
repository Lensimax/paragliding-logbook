import { apiClient } from '../../lib/api/client'
import type { CurrentUser, LoginPayload, RegisterPayload } from './types'

export const authApi = {
  register: (payload: RegisterPayload) => apiClient.post<CurrentUser>('/api/auth/register', payload),
  login: (payload: LoginPayload) => apiClient.post<CurrentUser>('/api/auth/login', payload),
  logout: () => apiClient.post<void>('/api/auth/logout'),
  me: () => apiClient.get<CurrentUser>('/api/me'),
}
