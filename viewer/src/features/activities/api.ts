import { apiClient } from '../../lib/api/client'
import type { ActivityDetail, ActivityListResponse, ActivityPayload } from './types'

export const activitiesApi = {
  list: (cursor?: string) =>
    apiClient.get<ActivityListResponse>(
      `/api/activities${cursor ? `?cursor=${encodeURIComponent(cursor)}` : ''}`,
    ),
  get: (id: string) => apiClient.get<ActivityDetail>(`/api/activities/${id}`),
  create: (payload: ActivityPayload) => apiClient.post<ActivityDetail>('/api/activities', payload),
  update: (id: string, payload: ActivityPayload) => apiClient.put<ActivityDetail>(`/api/activities/${id}`, payload),
  remove: (id: string) => apiClient.delete<void>(`/api/activities/${id}`),
}
