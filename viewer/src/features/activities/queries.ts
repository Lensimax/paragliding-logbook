import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { activitiesApi } from './api'
import type { ActivityPayload } from './types'

const activitiesKey = ['activities'] as const
const activityKey = (id: string) => ['activities', id] as const

export function useActivities() {
  return useInfiniteQuery({
    queryKey: activitiesKey,
    queryFn: ({ pageParam }: { pageParam: string | undefined }) => activitiesApi.list(pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  })
}

export function useActivity(id: string | undefined) {
  return useQuery({
    queryKey: activityKey(id ?? ''),
    queryFn: () => activitiesApi.get(id as string),
    enabled: id !== undefined,
  })
}

export function useCreateActivity() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: ActivityPayload) => activitiesApi.create(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: activitiesKey }),
  })
}

export function useUpdateActivity(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: ActivityPayload) => activitiesApi.update(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: activitiesKey })
      queryClient.invalidateQueries({ queryKey: activityKey(id) })
    },
  })
}

export function useDeleteActivity() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => activitiesApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: activitiesKey }),
  })
}

export function useUploadTrack(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => activitiesApi.uploadTrack(id, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: activitiesKey })
      queryClient.invalidateQueries({ queryKey: activityKey(id) })
    },
  })
}
