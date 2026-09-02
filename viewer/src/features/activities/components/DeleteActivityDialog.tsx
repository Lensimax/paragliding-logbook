import { Button } from '../../../components/ui/Button'
import { Dialog } from '../../../components/ui/Dialog'
import type { PanelNav } from '../../../lib/panel/types'
import { useDeleteActivity } from '../queries'

interface DeleteActivityDialogProps {
  activityId: string
  activityName: string
  nav: PanelNav
  onClose: () => void
}

export function DeleteActivityDialog({ activityId, activityName, nav, onClose }: DeleteActivityDialogProps) {
  const deleteActivity = useDeleteActivity()

  async function handleDelete() {
    await deleteActivity.mutateAsync(activityId)
    nav.pop()
  }

  return (
    <Dialog title="Delete activity" onClose={onClose}>
      <p>Delete “{activityName}”? This cannot be undone.</p>
      {deleteActivity.isError && <p className="field-error">Something went wrong. Please try again.</p>}
      <div className="ui-dialog-actions">
        <Button onClick={onClose} disabled={deleteActivity.isPending}>
          Abort
        </Button>
        <Button variant="primary" onClick={() => void handleDelete()} disabled={deleteActivity.isPending}>
          {deleteActivity.isPending ? 'Deleting…' : 'Delete'}
        </Button>
      </div>
    </Dialog>
  )
}
