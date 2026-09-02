import { apiClient, ApiError } from '../../lib/api/client'
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
  uploadTrack: async (id: string, file: File): Promise<ActivityDetail> => {
    const form = new FormData()
    form.append('file', file)

    // No Content-Type here: the browser sets the multipart boundary itself.
    const response = await fetch(`/api/activities/${id}/track`, {
      method: 'POST',
      credentials: 'include',
      body: form,
    })

    if (!response.ok) {
      const problem = await response.json().catch(() => null)
      throw new ApiError(
        response.status,
        problem?.title ?? problem?.message ?? `Request failed with status ${response.status}`,
        problem?.errors ?? {},
      )
    }

    return (await response.json()) as ActivityDetail
  },
}
