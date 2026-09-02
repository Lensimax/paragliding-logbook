import { useQuery } from '@tanstack/react-query'
import type { ElevationSample } from '../../lib/tracks/alignElevation'

async function fetchElevationSamples(activityId: string): Promise<ElevationSample[]> {
  const response = await fetch(`/api/activities/${activityId}/elevation`, { credentials: 'include' })
  if (!response.ok) throw new Error('Could not download ground elevation.')
  return (await response.json()) as ElevationSample[]
}

/** Ground elevation is resolved once per activity on upload (SPEC.md); cached forever client-side too. */
export function useElevationSamples(activityId: string | undefined, hasElevation: boolean | undefined) {
  return useQuery({
    queryKey: ['activities', activityId, 'elevation'],
    queryFn: () => fetchElevationSamples(activityId as string),
    enabled: activityId !== undefined && hasElevation === true,
    staleTime: Infinity,
  })
}
