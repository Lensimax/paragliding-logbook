import type { ReactNode } from 'react'
import { FlightMap } from '../../features/map/FlightMap'
import { AltitudeProfile } from '../../features/profile/AltitudeProfile'
import { EmptyProfile } from '../../features/profile/EmptyProfile'
import { useActivity } from '../../features/activities/queries'
import { useElevationSamples } from '../../features/activities/useElevation'
import { useSelectedActivity } from '../../features/activities/useSelectedActivity'
import { useTrack } from '../../features/activities/useTrack'
import { alignGroundElevation } from '../../lib/tracks/alignElevation'

export function MapStage() {
  const { selectedActivityId } = useSelectedActivity()
  const { data: activity } = useActivity(selectedActivityId ?? undefined)
  const { data: points } = useTrack(activity?.id, activity?.track?.format)
  const { data: elevationSamples } = useElevationSamples(activity?.id, activity?.hasElevation)

  const groundElevationM =
    points && elevationSamples && activity?.hasElevation ? alignGroundElevation(points, elevationSamples) : null

  const isTrackLoading = !!activity?.track && points === undefined
  const hasUsableTrack = !!activity?.track && !!points && points.length > 0 && points[0].time !== null

  let mapContent: ReactNode
  if (!activity) {
    mapContent = <p className="map-stage-placeholder">Select an activity with a track to see it on the map.</p>
  } else if (isTrackLoading) {
    mapContent = <p className="map-stage-placeholder">Loading track…</p>
  } else if (hasUsableTrack) {
    mapContent = <FlightMap points={points!} />
  } else {
    mapContent = (
      <p className="map-stage-placeholder">
        {activity.track ? 'This track has no timestamps to plot.' : 'This activity has no track.'}
      </p>
    )
  }

  return (
    <div className="map-stage">
      <div className="map-stage-map">{mapContent}</div>
      {activity &&
        (hasUsableTrack ? (
          <AltitudeProfile points={points!} groundElevationM={groundElevationM} />
        ) : (
          <EmptyProfile message="No track for this activity." />
        ))}
    </div>
  )
}
