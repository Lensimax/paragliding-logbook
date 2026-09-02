import { useState, type ChangeEvent, type FormEvent } from 'react'
import { Button } from '../../../components/ui/Button'
import { Field } from '../../../components/ui/Field'
import { ApiError } from '../../../lib/api/client'
import { browserTimeZone, fromDatetimeLocalValue, localDateOf, toDatetimeLocalValue } from '../../../lib/format/datetime'
import { formatDuration } from '../../../lib/format/duration'
import type { PanelNav, PanelView } from '../../../lib/panel/types'
import { parseGpx } from '../../../lib/tracks/parseGpx'
import { parseIgc } from '../../../lib/tracks/parseIgc'
import { computeTrackStats } from '../../../lib/tracks/stats'
import { makeActivityDetailView } from './ActivityDetail'
import { EquipmentPicker } from './EquipmentPicker'
import { useCreateActivity, useUpdateActivity } from '../queries'
import type { ActivityDetail as ActivityDetailModel, ActivityKind, ActivityPayload } from '../types'
import '../activities.css'

interface ActivityFormProps {
  nav: PanelNav
  activity?: ActivityDetailModel
}

export function ActivityForm({ nav, activity }: ActivityFormProps) {
  const isEdit = activity !== undefined
  const createActivity = useCreateActivity()
  const updateActivity = useUpdateActivity(activity?.id ?? '')

  const [type, setType] = useState<ActivityKind>(activity?.type ?? 'flight')
  const [name, setName] = useState(activity?.name ?? '')
  const [startedAtLocal, setStartedAtLocal] = useState(activity ? toDatetimeLocalValue(activity.startedAt) : '')
  const [endedAtLocal, setEndedAtLocal] = useState(activity?.endedAt ? toDatetimeLocalValue(activity.endedAt) : '')
  const [takeoffLocation, setTakeoffLocation] = useState(activity?.takeoffLocation ?? '')
  const [landingLocation, setLandingLocation] = useState(activity?.landingLocation ?? '')
  const [comment, setComment] = useState(activity?.comment ?? '')
  const [windSpeedKmh, setWindSpeedKmh] = useState(activity?.windSpeedKmh?.toString() ?? '')
  const [windDirection, setWindDirection] = useState(activity?.windDirection?.toString() ?? '')
  const [equipmentIds, setEquipmentIds] = useState<string[]>(activity?.equipmentIds ?? [])
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [trackError, setTrackError] = useState<string | null>(null)
  const [trackSummary, setTrackSummary] = useState<string | null>(null)

  const submitting = createActivity.isPending || updateActivity.isPending

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return

    setTrackError(null)
    setTrackSummary(null)

    try {
      const text = await file.text()
      const isIgc = file.name.toLowerCase().endsWith('.igc')
      const track = isIgc ? parseIgc(text) : parseGpx(text)
      const stats = computeTrackStats(track)

      setName(file.name.replace(/\.(gpx|igc)$/i, ''))
      if (stats.startedAt) setStartedAtLocal(toDatetimeLocalValue(stats.startedAt))
      if (stats.endedAt) setEndedAtLocal(toDatetimeLocalValue(stats.endedAt))

      const durationPart = stats.durationSeconds !== null ? `${formatDuration(stats.durationSeconds)}, ` : ''
      const noTimesNote = stats.startedAt === null ? ' (no timestamps in file — set start/end manually)' : ''
      setTrackSummary(
        `Parsed ${track.points.length} points — ${durationPart}${stats.distanceKm} km` +
          (stats.altitudeGainM !== null ? `, ${stats.altitudeGainM} m gain` : '') +
          noTimesNote,
      )
    } catch (error) {
      setTrackError(error instanceof Error ? error.message : 'Could not parse this file.')
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFieldErrors({})

    if (!name.trim()) {
      setFieldErrors({ name: ['Name is required.'] })
      return
    }
    if (!startedAtLocal) {
      setFieldErrors({ startedAt: ['Start datetime is required.'] })
      return
    }
    if (endedAtLocal && endedAtLocal <= startedAtLocal) {
      setFieldErrors({ endedAt: ['End datetime must be after the start datetime.'] })
      return
    }

    const payload: ActivityPayload = {
      type,
      name: name.trim(),
      startedAt: fromDatetimeLocalValue(startedAtLocal),
      endedAt: endedAtLocal ? fromDatetimeLocalValue(endedAtLocal) : null,
      localDate: localDateOf(startedAtLocal),
      localTz: browserTimeZone(),
      takeoffLocation: takeoffLocation.trim() || null,
      landingLocation: landingLocation.trim() || null,
      comment: comment.trim() || null,
      windSpeedKmh: type === 'groundHandling' && windSpeedKmh ? Number(windSpeedKmh) : null,
      windDirection: type === 'groundHandling' && windDirection ? Number(windDirection) : null,
      equipmentIds,
    }

    try {
      if (isEdit) {
        await updateActivity.mutateAsync(payload)
        nav.pop()
      } else {
        const created = await createActivity.mutateAsync(payload)
        nav.replace(makeActivityDetailView(created.id))
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
    <form className="activity-form" onSubmit={handleSubmit} noValidate>
      <Field label="Type" htmlFor="activity-type">
        <select id="activity-type" value={type} onChange={(e) => setType(e.target.value as ActivityKind)}>
          <option value="flight">Flight</option>
          <option value="groundHandling">Ground handling</option>
        </select>
      </Field>

      {!isEdit && (
        <Field label="Flight file (GPX or IGC)" htmlFor="activity-track-file" error={trackError ?? undefined}>
          <input
            id="activity-track-file"
            type="file"
            accept=".gpx,.igc"
            onChange={(e) => void handleFileChange(e)}
          />
          {trackSummary && <p className="track-summary">{trackSummary}</p>}
        </Field>
      )}

      <Field label="Name" htmlFor="activity-name" error={fieldErrors.name?.[0]}>
        <input id="activity-name" value={name} onChange={(e) => setName(e.target.value)} required />
      </Field>

      <Field label="Start datetime" htmlFor="activity-started" error={fieldErrors.startedAt?.[0]}>
        <input
          id="activity-started"
          type="datetime-local"
          value={startedAtLocal}
          onChange={(e) => setStartedAtLocal(e.target.value)}
          required
        />
      </Field>

      <Field label="End datetime" htmlFor="activity-ended" error={fieldErrors.endedAt?.[0]}>
        <input
          id="activity-ended"
          type="datetime-local"
          value={endedAtLocal}
          onChange={(e) => setEndedAtLocal(e.target.value)}
        />
      </Field>

      <Field label="Takeoff location" htmlFor="activity-takeoff">
        <input id="activity-takeoff" value={takeoffLocation} onChange={(e) => setTakeoffLocation(e.target.value)} />
      </Field>

      <Field label="Landing location" htmlFor="activity-landing">
        <input id="activity-landing" value={landingLocation} onChange={(e) => setLandingLocation(e.target.value)} />
      </Field>

      {type === 'groundHandling' && (
        <>
          <Field label="Wind speed (km/h)" htmlFor="activity-wind-speed" error={fieldErrors.windSpeedKmh?.[0]}>
            <input
              id="activity-wind-speed"
              type="number"
              min={0}
              value={windSpeedKmh}
              onChange={(e) => setWindSpeedKmh(e.target.value)}
            />
          </Field>
          <Field label="Wind direction (°)" htmlFor="activity-wind-direction" error={fieldErrors.windDirection?.[0]}>
            <input
              id="activity-wind-direction"
              type="number"
              min={0}
              max={359}
              value={windDirection}
              onChange={(e) => setWindDirection(e.target.value)}
            />
          </Field>
        </>
      )}

      <EquipmentPicker selectedIds={equipmentIds} onChange={setEquipmentIds} autoSelectDefaults={!isEdit} />

      <Field label="Comment" htmlFor="activity-comment">
        <textarea id="activity-comment" value={comment} onChange={(e) => setComment(e.target.value)} />
      </Field>

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

export function makeActivityCreateView(): PanelView {
  return {
    key: 'activity-create',
    title: 'Create activity',
    render: (nav) => <ActivityForm nav={nav} />,
  }
}

export function makeActivityEditView(activity: ActivityDetailModel): PanelView {
  return {
    key: `activity-edit-${activity.id}`,
    title: 'Edit activity',
    render: (nav) => <ActivityForm nav={nav} activity={activity} />,
  }
}
