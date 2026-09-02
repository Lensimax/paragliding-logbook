import { useQuery } from '@tanstack/react-query'
import { parseGpx } from '../../lib/tracks/parseGpx'
import { parseIgc } from '../../lib/tracks/parseIgc'
import type { TrackPoint } from '../../lib/tracks/types'

async function fetchTrackPoints(activityId: string, format: 'gpx' | 'igc'): Promise<TrackPoint[]> {
  const response = await fetch(`/api/activities/${activityId}/track`, { credentials: 'include' })
  if (!response.ok) throw new Error('Could not download track.')

  const text = await response.text()
  const track = format === 'igc' ? parseIgc(text) : parseGpx(text)
  return track.points
}

/** Downloads and parses an activity's track client-side. Nothing is stored server-side but the raw bytes. */
export function useTrack(activityId: string | undefined, format: 'gpx' | 'igc' | undefined) {
  return useQuery({
    queryKey: ['activities', activityId, 'track'],
    queryFn: () => fetchTrackPoints(activityId as string, format as 'gpx' | 'igc'),
    enabled: activityId !== undefined && format !== undefined,
    staleTime: Infinity, // a track's content never changes without a new activity id
  })
}
