import { apiClient } from '../../lib/api/client'
import type { EquipmentDetail, EquipmentPayload, EquipmentSummary } from './types'

export const equipmentApi = {
  list: () => apiClient.get<EquipmentSummary[]>('/api/equipment'),
  get: (id: string) => apiClient.get<EquipmentDetail>(`/api/equipment/${id}`),
  create: (payload: EquipmentPayload) => apiClient.post<EquipmentDetail>('/api/equipment', payload),
  update: (id: string, payload: EquipmentPayload) => apiClient.put<EquipmentDetail>(`/api/equipment/${id}`, payload),
  remove: (id: string) => apiClient.delete<void>(`/api/equipment/${id}`),
  retire: (id: string) => apiClient.post<EquipmentDetail>(`/api/equipment/${id}/retire`),
}
