import { useState } from 'react'
import { Dropdown } from '../../../components/ui/Dropdown'
import { EmptyState } from '../../../components/ui/EmptyState'
import { formatDateTime } from '../../../lib/format/datetime'
import { formatDuration } from '../../../lib/format/duration'
import type { PanelNav, PanelView } from '../../../lib/panel/types'
import { makeActivityEditView } from './ActivityForm'
import { DeleteActivityDialog } from './DeleteActivityDialog'
import { useActivity } from '../queries'
import { useEquipmentList } from '../../equipment/queries'
import '../activities.css'

interface ActivityDetailProps {
  activityId: string
  nav: PanelNav
}

export function ActivityDetail({ activityId, nav }: ActivityDetailProps) {
  const { data: activity, isLoading, isError } = useActivity(activityId)
  const { data: equipmentList } = useEquipmentList()
  const [deleting, setDeleting] = useState(false)

  if (isLoading) return <p>Loading…</p>

  if (isError || !activity) {
    return <EmptyState title="Activity not found" description="It may have been deleted." />
  }

  const equipmentNames = activity.equipmentIds
    .map((id) => equipmentList?.find((item) => item.id === id)?.displayName)
    .filter((name): name is string => Boolean(name))

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
