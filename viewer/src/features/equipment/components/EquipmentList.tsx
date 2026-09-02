import { Button } from '../../../components/ui/Button'
import { EmptyState } from '../../../components/ui/EmptyState'
import type { PanelNav } from '../../../lib/panel/types'
import { equipmentTypeLabels } from '../labels'
import { useEquipmentList } from '../queries'
import { makeEquipmentDetailView } from './EquipmentDetail'
import { makeEquipmentCreateView } from './EquipmentForm'
import '../equipment.css'

interface EquipmentListProps {
  nav: PanelNav
}

export function EquipmentList({ nav }: EquipmentListProps) {
  const { data, isLoading, isError } = useEquipmentList()

  return (
    <div className="equipment-list">
      {isLoading ? (
        <p>Loading…</p>
      ) : isError ? (
        <EmptyState title="Couldn't load equipment" description="Please try again." />
      ) : !data || data.length === 0 ? (
        <EmptyState title="No equipment yet" description="Add your wing, harness or other gear." />
      ) : (
        <ul className="equipment-list-scroll">
          {data.map((item) => (
            <li key={item.id}>
              <button
                type="button"
                className="equipment-list-item"
                onClick={() => nav.push(makeEquipmentDetailView(item.id))}
              >
                <span className="equipment-list-item-name">
                  {item.displayName}
                  {item.retired && <span className="equipment-retired-badge"> (retired)</span>}
                </span>
                <span className="equipment-list-item-type">{equipmentTypeLabels[item.type]}</span>
              </button>
            </li>
          ))}
        </ul>
      )}

      <div className="equipment-list-footer">
        <Button variant="primary" onClick={() => nav.push(makeEquipmentCreateView())}>
          Create equipment
        </Button>
      </div>
    </div>
  )
}
