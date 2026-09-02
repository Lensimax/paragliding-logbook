import { useEffect, useRef } from 'react'
import { useEquipmentList } from '../../equipment/queries'

interface EquipmentPickerProps {
  selectedIds: string[]
  onChange: (ids: string[]) => void
  /** Pre-select "add automatically" equipment once loaded. Only meaningful when creating. */
  autoSelectDefaults?: boolean
}

export function EquipmentPicker({ selectedIds, onChange, autoSelectDefaults = false }: EquipmentPickerProps) {
  const { data, isLoading } = useEquipmentList()
  const autoApplied = useRef(false)

  const active = (data ?? []).filter((item) => !item.retired)

  useEffect(() => {
    if (!autoSelectDefaults || autoApplied.current || !data) return
    autoApplied.current = true
    const defaults = active.filter((item) => item.autoAdd).map((item) => item.id)
    if (defaults.length > 0) onChange(defaults)
    // Runs once, the first time equipment finishes loading.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data, autoSelectDefaults])

  function toggle(id: string) {
    onChange(selectedIds.includes(id) ? selectedIds.filter((existingId) => existingId !== id) : [...selectedIds, id])
  }

  return (
    <div className="ui-field">
      <span>Equipment</span>
      {isLoading ? (
        <p className="equipment-picker-empty">Loading…</p>
      ) : active.length === 0 ? (
        <p className="equipment-picker-empty">No equipment yet — add some from User Settings.</p>
      ) : (
        <ul className="equipment-picker-list">
          {active.map((item) => (
            <li key={item.id}>
              <label>
                <input type="checkbox" checked={selectedIds.includes(item.id)} onChange={() => toggle(item.id)} />
                {item.displayName}
              </label>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
