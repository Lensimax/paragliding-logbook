interface EquipmentPickerProps {
  selectedIds: string[]
  onChange: (ids: string[]) => void
}

// Equipment CRUD lands in Step 6; until then there is nothing to pick from.
export function EquipmentPicker(_props: EquipmentPickerProps) {
  return (
    <div className="ui-field">
      <span>Equipment</span>
      <p className="equipment-picker-empty">No equipment yet — add some from User Settings once available.</p>
    </div>
  )
}
