import { EmptyState } from '../../../components/ui/EmptyState'
import { Button } from '../../../components/ui/Button'
import type { PanelNav, PanelView } from '../../../lib/panel/types'

const createActivityView: PanelView = {
  key: 'activity-create',
  title: 'Create activity',
  render: () => (
    <EmptyState title="Coming soon" description="Creating activities arrives in a later step." />
  ),
}

interface ActivityListProps {
  nav: PanelNav
}

export function ActivityList({ nav }: ActivityListProps) {
  return (
    <div className="activity-list">
      <EmptyState
        title="No activities yet"
        description="Log a flight or ground handling session to get started."
      />
      <div className="activity-list-footer">
        <Button variant="primary" onClick={() => nav.push(createActivityView)}>
          Create/import
        </Button>
      </div>
    </div>
  )
}
