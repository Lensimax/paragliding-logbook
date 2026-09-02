import { useState } from 'react'
import { Dropdown } from '../../../components/ui/Dropdown'
import { EmptyState } from '../../../components/ui/EmptyState'
import type { PanelNav, PanelView } from '../../../lib/panel/types'
import { equipmentTypeLabels } from '../labels'
import { useEquipmentDetail } from '../queries'
import { DeleteEquipmentDialog } from './DeleteEquipmentDialog'
import { makeEquipmentEditView } from './EquipmentForm'
import { UsageSection } from './UsageSection'
import '../equipment.css'

interface EquipmentDetailProps {
  equipmentId: string
  nav: PanelNav
}

export function EquipmentDetail({ equipmentId, nav }: EquipmentDetailProps) {
  const { data: equipment, isLoading, isError } = useEquipmentDetail(equipmentId)
  const [deleting, setDeleting] = useState(false)

  if (isLoading) return <p>Loading…</p>

  if (isError || !equipment) {
    return <EmptyState title="Equipment not found" description="It may have been deleted." />
  }

  return (
    <div className="equipment-detail">
      <div className="equipment-detail-toolbar">
        <Dropdown label="Equipment actions">
          {(close) => (
            <>
              <button
                type="button"
                onClick={() => {
                  close()
                  nav.push(makeEquipmentEditView(equipment))
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

      <h2>
        {equipment.displayName}
        {equipment.retired && <span className="equipment-retired-badge"> (retired)</span>}
      </h2>

      <dl>
        <dt>Type</dt>
        <dd>{equipmentTypeLabels[equipment.type]}</dd>

        {equipment.brand && (
          <>
            <dt>Brand</dt>
            <dd>{equipment.brand}</dd>
          </>
        )}

        {equipment.model && (
          <>
            <dt>Model</dt>
            <dd>{equipment.model}</dd>
          </>
        )}

        {equipment.purchaseDate && (
          <>
            <dt>Purchased</dt>
            <dd>{equipment.purchaseDate}</dd>
          </>
        )}

        {equipment.nextRevisionDate && (
          <>
            <dt>Next revision</dt>
            <dd>{equipment.nextRevisionDate}</dd>
          </>
        )}

        <dt>Add automatically</dt>
        <dd>{equipment.autoAdd ? 'Yes' : 'No'}</dd>
      </dl>

      {equipment.revisions.length > 0 && (
        <div className="equipment-revisions">
          <h3>Revisions</h3>
          <ul>
            {equipment.revisions.map((revision) => (
              <li key={revision.id}>
                {revision.revisionDate}
                {revision.comment && ` — ${revision.comment}`}
              </li>
            ))}
          </ul>
        </div>
      )}

      <UsageSection usage={equipment.usage} />

      {deleting && <DeleteEquipmentDialog equipment={equipment} nav={nav} onClose={() => setDeleting(false)} />}
    </div>
  )
}

export function makeEquipmentDetailView(equipmentId: string): PanelView {
  return {
    key: `equipment-detail-${equipmentId}`,
    title: 'Equipment',
    render: (nav) => <EquipmentDetail equipmentId={equipmentId} nav={nav} />,
  }
}
