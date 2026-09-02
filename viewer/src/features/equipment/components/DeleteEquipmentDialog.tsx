import { Button } from '../../../components/ui/Button'
import { Dialog } from '../../../components/ui/Dialog'
import type { PanelNav } from '../../../lib/panel/types'
import { useDeleteEquipment, useRetireEquipment } from '../queries'
import type { EquipmentDetail } from '../types'

interface DeleteEquipmentDialogProps {
  equipment: EquipmentDetail
  nav: PanelNav
  onClose: () => void
}

export function DeleteEquipmentDialog({ equipment, nav, onClose }: DeleteEquipmentDialogProps) {
  const deleteEquipment = useDeleteEquipment()
  const retireEquipment = useRetireEquipment()
  const inUse = equipment.usage.activityCount > 0
  const pending = deleteEquipment.isPending || retireEquipment.isPending

  async function handleDelete() {
    await deleteEquipment.mutateAsync(equipment.id)
    nav.pop()
  }

  async function handleRetire() {
    await retireEquipment.mutateAsync(equipment.id)
    onClose()
  }

  return (
    <Dialog title="Delete equipment" onClose={onClose}>
      <p>
        “{equipment.displayName}” is used by {equipment.usage.activityCount} activit
        {equipment.usage.activityCount === 1 ? 'y' : 'ies'} ({equipment.usage.hours.toFixed(1)}h).
      </p>
      {inUse ? (
        <p>Deleting would rewrite the logbook, so it's refused. Retire it instead to hide it from pickers.</p>
      ) : (
        <p>This cannot be undone.</p>
      )}
      {(deleteEquipment.isError || retireEquipment.isError) && (
        <p className="field-error">Something went wrong. Please try again.</p>
      )}
      <div className="ui-dialog-actions">
        <Button onClick={onClose} disabled={pending}>
          Abort
        </Button>
        {inUse ? (
          <Button variant="primary" onClick={() => void handleRetire()} disabled={pending}>
            {retireEquipment.isPending ? 'Retiring…' : 'Retire'}
          </Button>
        ) : (
          <Button variant="primary" onClick={() => void handleDelete()} disabled={pending}>
            {deleteEquipment.isPending ? 'Deleting…' : 'Delete'}
          </Button>
        )}
      </div>
    </Dialog>
  )
}
