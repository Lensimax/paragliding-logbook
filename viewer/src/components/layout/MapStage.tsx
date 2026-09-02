import { FlightMap } from '../../features/map/FlightMap'
import { useActivity } from '../../features/activities/queries'
import { useSelectedActivity } from '../../features/activities/useSelectedActivity'
import { useTrack } from '../../features/activities/useTrack'

export function MapStage() {
  const { selectedActivityId } = useSelectedActivity()
  const { data: activity } = useActivity(selectedActivityId ?? undefined)
  const { data: points } = useTrack(activity?.id, activity?.track?.format)

  if (!activity?.track) {
    return (
      <div className="map-stage">
        <p className="map-stage-placeholder">
          {selectedActivityId
            ? 'This activity has no track. The altitude profile arrives in a later step.'
            : 'Select an activity with a track to see it on the map.'}
        </p>
      </div>
    )
  }

  return (
    <div className="map-stage">
      <FlightMap points={points ?? []} />
    </div>
  )
}
