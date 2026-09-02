import { useEffect, useState, type ChangeEvent } from 'react'
import { Dropdown } from '../../../components/ui/Dropdown'
import { EmptyState } from '../../../components/ui/EmptyState'
import { formatDateTime } from '../../../lib/format/datetime'
import { formatDuration } from '../../../lib/format/duration'
import type { PanelNav, PanelView } from '../../../lib/panel/types'
import { makeActivityEditView } from './ActivityForm'
import { DeleteActivityDialog } from './DeleteActivityDialog'
import { useActivity, useUploadTrack } from '../queries'
import { useSelectedActivity } from '../useSelectedActivity'
import { useEquipmentList } from '../../equipment/queries'
import '../activities.css'

interface ActivityDetailProps {
  activityId: string
  nav: PanelNav
}

export function ActivityDetail({ activityId, nav }: ActivityDetailProps) {
  const { data: activity, isLoading, isError } = useActivity(activityId)
  const { data: equipmentList } = useEquipmentList()
  const uploadTrack = useUploadTrack(activityId)
  const { setSelectedActivityId } = useSelectedActivity()
  const [deleting, setDeleting] = useState(false)

  useEffect(() => {
    setSelectedActivityId(activityId)
    return () => setSelectedActivityId(null)
  }, [activityId, setSelectedActivityId])

  if (isLoading) return <p>Loading…</p>

  if (isError || !activity) {
    return <EmptyState title="Activity not found" description="It may have been deleted." />
  }

  const equipmentNames = activity.equipmentIds
    .map((id) => equipmentList?.find((item) => item.id === id)?.displayName)
    .filter((name): name is string => Boolean(name))

  async function handleTrackFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return
    await uploadTrack.mutateAsync(file)
    event.target.value = ''
  }

  return (
    <div className="activity-detail">
      <div className="activity-detail-toolbar">
        <Dropdown label="Activity actions">
          {(close) => (
            <>
              <button
                type="button"
                onClick={() => {
                  close()
                  nav.push(makeActivityEditView(activity))
                }}
              >
                Edit
              </button>
              <button
                type="button"
                className="destructive"
                onClick={() => {
                  close()
                  setDeleting(true)
                }}
              >
                Delete
              </button>
            </>
          )}
        </Dropdown>
      </div>

      <h2>{activity.name}</h2>

      <dl>
        <dt>Start</dt>
        <dd>{formatDateTime(activity.startedAt)}</dd>

        <dt>Duration</dt>
        <dd>{formatDuration(activity.durationSeconds)}</dd>

        <dt>Equipment</dt>
        <dd>{activity.equipmentIds.length === 0 ? 'None' : equipmentNames.join(', ') || activity.equipmentIds.length}</dd>

        {activity.comment && (
          <>
            <dt>Comment</dt>
            <dd>{activity.comment}</dd>
          </>
        )}
      </dl>

      {activity.type === 'flight' && (
        <div className="activity-track-section">
          {activity.track ? (
            <p className="track-summary">Track: {activity.track.filename}</p>
          ) : (
            <>
              <p className="track-summary">No track uploaded.</p>
              <input
                type="file"
                accept=".gpx,.igc"
                aria-label="Upload track"
                onChange={(e) => void handleTrackFileChange(e)}
                disabled={uploadTrack.isPending}
              />
              {uploadTrack.isPending && <p className="track-summary">Uploading…</p>}
              {uploadTrack.isError && <p className="field-error">Upload failed. Please try again.</p>}
            </>
          )}
        </div>
      )}

      {deleting && (
        <DeleteActivityDialog
          activityId={activity.id}
          activityName={activity.name}
          nav={nav}
          onClose={() => setDeleting(false)}
        />
      )}
    </div>
  )
}

export function makeActivityDetailView(activityId: string): PanelView {
  return {
    key: `activity-detail-${activityId}`,
    title: 'Activity',
    render: (nav) => <ActivityDetail activityId={activityId} nav={nav} />,
  }
}
