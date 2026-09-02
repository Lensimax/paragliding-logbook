import { useState, type FormEvent } from 'react'
import { Button } from '../../../components/ui/Button'
import { Field } from '../../../components/ui/Field'
import { ApiError } from '../../../lib/api/client'
import type { PanelNav, PanelView } from '../../../lib/panel/types'
import { equipmentTypeLabels } from '../labels'
import { makeEquipmentDetailView } from './EquipmentDetail'
import { useCreateEquipment, useUpdateEquipment } from '../queries'
import type {
  EquipmentDetail as EquipmentDetailModel,
  EquipmentKind,
  EquipmentPayload,
  RevisionInput,
} from '../types'
import '../equipment.css'

interface EquipmentFormProps {
  nav: PanelNav
  equipment?: EquipmentDetailModel
}

export function EquipmentForm({ nav, equipment }: EquipmentFormProps) {
  const isEdit = equipment !== undefined
  const createEquipment = useCreateEquipment()
  const updateEquipment = useUpdateEquipment(equipment?.id ?? '')

  const [displayName, setDisplayName] = useState(equipment?.displayName ?? '')
  const [type, setType] = useState<EquipmentKind>(equipment?.type ?? 'wing')
  const [brand, setBrand] = useState(equipment?.brand ?? '')
  const [model, setModel] = useState(equipment?.model ?? '')
  const [purchaseDate, setPurchaseDate] = useState(equipment?.purchaseDate ?? '')
  const [nextRevisionDate, setNextRevisionDate] = useState(equipment?.nextRevisionDate ?? '')
  const [autoAdd, setAutoAdd] = useState(equipment?.autoAdd ?? false)
  const [revisions, setRevisions] = useState<RevisionInput[]>(
    equipment?.revisions.map((r) => ({ revisionDate: r.revisionDate, comment: r.comment })) ?? [],
  )
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  const submitting = createEquipment.isPending || updateEquipment.isPending

  function updateRevision(index: number, patch: Partial<RevisionInput>) {
    setRevisions((rows) => rows.map((row, i) => (i === index ? { ...row, ...patch } : row)))
  }

  function addRevision() {
    setRevisions((rows) => [...rows, { revisionDate: '', comment: '' }])
  }

  function removeRevision(index: number) {
    setRevisions((rows) => rows.filter((_, i) => i !== index))
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFieldErrors({})

    if (!displayName.trim()) {
      setFieldErrors({ displayName: ['Display name is required.'] })
      return
    }
    if (revisions.some((r) => !r.revisionDate)) {
      setFieldErrors({ revisions: ['Every revision needs a date.'] })
      return
    }

    const payload: EquipmentPayload = {
      displayName: displayName.trim(),
      type,
      brand: brand.trim() || null,
      model: model.trim() || null,
      purchaseDate: purchaseDate || null,
      nextRevisionDate: nextRevisionDate || null,
      autoAdd,
      revisions: revisions.map((r) => ({ revisionDate: r.revisionDate, comment: r.comment?.trim() || null })),
    }

    try {
      if (isEdit) {
        await updateEquipment.mutateAsync(payload)
        nav.pop()
      } else {
        const created = await createEquipment.mutateAsync(payload)
        nav.replace(makeEquipmentDetailView(created.id))
      }
    } catch (error) {
      if (error instanceof ApiError) {
        setFieldErrors(Object.keys(error.fieldErrors).length > 0 ? error.fieldErrors : { _: [error.message] })
      } else {
        setFieldErrors({ _: ['Something went wrong. Please try again.'] })
      }
    }
  }

  return (
    <form className="equipment-form" onSubmit={handleSubmit} noValidate>
      <Field label="Display name" htmlFor="equipment-name" error={fieldErrors.displayName?.[0]}>
        <input id="equipment-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
      </Field>

      <Field label="Type" htmlFor="equipment-type">
        <select id="equipment-type" value={type} onChange={(e) => setType(e.target.value as EquipmentKind)}>
          {Object.entries(equipmentTypeLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </Field>

      <Field label="Brand" htmlFor="equipment-brand">
        <input id="equipment-brand" value={brand} onChange={(e) => setBrand(e.target.value)} />
      </Field>

      <Field label="Model" htmlFor="equipment-model">
        <input id="equipment-model" value={model} onChange={(e) => setModel(e.target.value)} />
      </Field>

      <Field label="Purchase date" htmlFor="equipment-purchase-date">
        <input
          id="equipment-purchase-date"
          type="date"
          value={purchaseDate}
          onChange={(e) => setPurchaseDate(e.target.value)}
        />
      </Field>

      <Field label="Next revision date" htmlFor="equipment-next-revision">
        <input
          id="equipment-next-revision"
          type="date"
          value={nextRevisionDate}
          onChange={(e) => setNextRevisionDate(e.target.value)}
        />
      </Field>

      <label className="equipment-auto-add">
        <input type="checkbox" checked={autoAdd} onChange={(e) => setAutoAdd(e.target.checked)} />
        Add automatically to new activities
      </label>

      <fieldset className="equipment-revisions-editor">
        <legend>Revision dates</legend>
        {fieldErrors.revisions?.[0] && <p className="field-error">{fieldErrors.revisions[0]}</p>}
        {revisions.map((revision, index) => (
          <div className="equipment-revision-row" key={index}>
            <input
              type="date"
              value={revision.revisionDate}
              onChange={(e) => updateRevision(index, { revisionDate: e.target.value })}
              aria-label="Revision date"
            />
            <input
              type="text"
              placeholder="Comment (optional)"
              value={revision.comment ?? ''}
              onChange={(e) => updateRevision(index, { comment: e.target.value })}
              aria-label="Revision comment"
            />
            <Button variant="icon" aria-label="Remove revision" onClick={() => removeRevision(index)}>
              ✕
            </Button>
          </div>
        ))}
        <Button type="button" onClick={addRevision}>
          Add revision
        </Button>
      </fieldset>

      {fieldErrors._?.map((message) => (
        <p className="field-error" key={message}>
          {message}
        </p>
      ))}

      <Button variant="primary" type="submit" disabled={submitting}>
        {submitting ? 'Saving…' : isEdit ? 'Save' : 'Create'}
      </Button>
    </form>
  )
}

export function makeEquipmentCreateView(): PanelView {
  return {
    key: 'equipment-create',
    title: 'Create equipment',
    render: (nav) => <EquipmentForm nav={nav} />,
  }
}

export function makeEquipmentEditView(equipment: EquipmentDetailModel): PanelView {
  return {
    key: `equipment-edit-${equipment.id}`,
    title: 'Edit equipment',
    render: (nav) => <EquipmentForm nav={nav} equipment={equipment} />,
  }
}
