import { apiClient } from '../../lib/api/client'
import type { CurrentUser, LoginPayload, RegisterPayload, RegisteredUser } from './types'

export const authApi = {
  register: (payload: RegisterPayload) => apiClient.post<RegisteredUser>('/api/auth/register', payload),
  login: (payload: LoginPayload) => apiClient.post<RegisteredUser>('/api/auth/login', payload),
  logout: () => apiClient.post<void>('/api/auth/logout'),
  me: () => apiClient.get<CurrentUser>('/api/me'),
}
