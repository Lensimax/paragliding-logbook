import { formatDateTime } from '../../../lib/format/datetime'
import { formatDuration } from '../../../lib/format/duration'
import type { ActivitySummary } from '../types'

interface ActivityListItemProps {
  activity: ActivitySummary
  onSelect: () => void
}

export function ActivityListItem({ activity, onSelect }: ActivityListItemProps) {
  return (
    <li>
      <button type="button" className="activity-list-item" onClick={onSelect}>
        <span className="activity-list-item-icon" aria-hidden="true">
          {activity.type === 'flight' ? '🪂' : '🏃'}
        </span>
        <span className="activity-list-item-main">
          <span className="activity-list-item-name">{activity.name}</span>
          <span className="activity-list-item-meta">{formatDateTime(activity.startedAt)}</span>
        </span>
        <span className="activity-list-item-duration">{formatDuration(activity.durationSeconds)}</span>
      </button>
    </li>
  )
}
