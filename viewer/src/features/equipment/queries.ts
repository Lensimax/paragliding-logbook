import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { equipmentApi } from './api'
import type { EquipmentPayload } from './types'

const equipmentListKey = ['equipment'] as const
const equipmentKey = (id: string) => ['equipment', id] as const

export function useEquipmentList() {
  return useQuery({ queryKey: equipmentListKey, queryFn: equipmentApi.list })
}

export function useEquipmentDetail(id: string | undefined) {
  return useQuery({
    queryKey: equipmentKey(id ?? ''),
    queryFn: () => equipmentApi.get(id as string),
    enabled: id !== undefined,
  })
}

export function useCreateEquipment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: EquipmentPayload) => equipmentApi.create(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: equipmentListKey }),
  })
}

export function useUpdateEquipment(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: EquipmentPayload) => equipmentApi.update(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: equipmentListKey })
      queryClient.invalidateQueries({ queryKey: equipmentKey(id) })
    },
  })
}

export function useDeleteEquipment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => equipmentApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: equipmentListKey }),
  })
}

export function useRetireEquipment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => equipmentApi.retire(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: equipmentListKey }),
  })
}
