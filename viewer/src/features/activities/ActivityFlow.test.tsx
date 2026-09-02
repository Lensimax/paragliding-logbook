import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../../App'
import { toDatetimeLocalValue } from '../../lib/format/datetime'
import sampleGpx from '../../lib/tracks/fixtures/sample.gpx?raw'

const currentUser = {
  id: 'user-1',
  publicId: 'bob-ab12x',
  username: 'bob',
  email: 'bob@example.com',
}

interface StoredTrack {
  filename: string
  format: 'gpx' | 'igc'
  sizeBytes: number
  sha256: string
  content: string
}

interface StoredActivity {
  id: string
  type: string
  name: string
  startedAt: string
  endedAt: string | null
  localDate: string
  localTz: string | null
  takeoffLocation: string | null
  landingLocation: string | null
  comment: string | null
  windSpeedKmh: number | null
  windDirection: number | null
  durationSeconds: number | null
  track: StoredTrack | null
  equipmentIds: string[]
}

function jsonResponse(body: unknown, status = 200) {
  return { ok: status < 400, status, json: async () => body }
}

function toResponseBody(activity: StoredActivity) {
  const { track, ...rest } = activity
  return {
    ...rest,
    track: track ? { filename: track.filename, format: track.format, sizeBytes: track.sizeBytes, sha256: track.sha256 } : null,
  }
}

function installFakeBackend() {
  const activities = new Map<string, StoredActivity>()
  let nextId = 1

  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString()
      const method = (init?.method ?? 'GET').toUpperCase()

      if (url.includes('/api/me')) return jsonResponse(currentUser)

      if (url.includes('/api/activities')) {
        const trackMatch = /\/api\/activities\/([^/?]+)\/track$/.exec(url)
        const idMatch = !trackMatch && /\/api\/activities\/([^/?]+)/.exec(url)

        if (trackMatch) {
          const activity = activities.get(trackMatch[1])

          if (method === 'POST') {
            if (!activity) return jsonResponse({}, 404)
            const form = init?.body as FormData
            const file = form.get('file') as File
            const content = await file.text()
            activity.track = {
              filename: `track.${file.name.toLowerCase().endsWith('.igc') ? 'igc' : 'gpx'}`,
              format: file.name.toLowerCase().endsWith('.igc') ? 'igc' : 'gpx',
              sizeBytes: content.length,
              sha256: 'fake-hash',
              content,
            }
            return jsonResponse(toResponseBody(activity))
          }

          if (method === 'GET') {
            if (!activity?.track) return jsonResponse({}, 404)
            return {
              ok: true,
              status: 200,
              text: async () => activity.track!.content,
              headers: new Headers({ 'Content-Type': 'application/gpx+xml' }),
            }
          }
        }

        if (idMatch && method === 'GET') {
          const activity = activities.get(idMatch[1])
          return activity ? jsonResponse(toResponseBody(activity)) : jsonResponse({}, 404)
        }

        if (idMatch && method === 'DELETE') {
          activities.delete(idMatch[1])
          return { ok: true, status: 204, json: async () => undefined }
        }

        if (!idMatch && method === 'POST') {
          const body = JSON.parse(init?.body as string)
          const id = `activity-${nextId++}`
          const created: StoredActivity = {
            id,
            type: body.type,
            name: body.name,
            startedAt: body.startedAt,
            endedAt: body.endedAt ?? null,
            localDate: body.localDate,
            localTz: body.localTz ?? null,
            takeoffLocation: body.takeoffLocation ?? null,
            landingLocation: body.landingLocation ?? null,
            comment: body.comment ?? null,
            windSpeedKmh: body.windSpeedKmh ?? null,
            windDirection: body.windDirection ?? null,
            durationSeconds: null,
            track: null,
            equipmentIds: body.equipmentIds ?? [],
          }
          activities.set(id, created)
          return jsonResponse(toResponseBody(created), 201)
        }

        if (!idMatch && method === 'GET') {
          const items = [...activities.values()]
            .sort((a, b) => b.startedAt.localeCompare(a.startedAt))
            .map((a) => ({
              id: a.id,
              type: a.type,
              name: a.name,
              startedAt: a.startedAt,
              endedAt: a.endedAt,
              durationSeconds: a.durationSeconds,
            }))
          return jsonResponse({ items, nextCursor: null })
        }
      }

      if (url.includes('/api/equipment')) return jsonResponse([])

      return jsonResponse({}, 404)
    }),
  )

  return activities
}

describe('activity CRUD flow', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('creates an activity and lands on its detail view, then deletes it back to the empty list', async () => {
    installFakeBackend()
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)

    await user.click(screen.getByRole('button', { name: 'Create/import' }))
    await screen.findByRole('heading', { name: 'Create activity' })

    await user.type(screen.getByLabelText('Name'), 'Evening glide')
    await user.type(screen.getByLabelText('Start datetime'), '2026-03-05T14:30')
    await user.click(screen.getByRole('button', { name: 'Create' }))

    expect(await screen.findByRole('heading', { name: 'Evening glide' })).toBeInTheDocument()

    // Back to the list shows the new activity.
    await user.click(screen.getByRole('button', { name: /back/i }))
    expect(await screen.findByText('Evening glide')).toBeInTheDocument()

    // Select it again, then delete it.
    await user.click(screen.getByText('Evening glide'))
    expect(await screen.findByRole('heading', { name: 'Evening glide' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Activity actions' }))
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    const dialog = await screen.findByRole('dialog', { name: 'Delete activity' })
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(screen.getByText(/no activities yet/i)).toBeInTheDocument(), { timeout: 3000 })
  })

  it('auto-fills name and datetimes from an uploaded GPX file', async () => {
    installFakeBackend()
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)

    await user.click(screen.getByRole('button', { name: 'Create/import' }))
    await screen.findByRole('heading', { name: 'Create activity' })

    const file = new File([sampleGpx], 'sample.gpx', { type: 'application/gpx+xml' })
    await user.upload(screen.getByLabelText('Flight file (GPX or IGC)'), file)

    expect(await screen.findByText(/parsed 10 points/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Name')).toHaveValue('sample')
    // Local-time values, so compare via the same conversion rather than hardcoding a timezone.
    expect(screen.getByLabelText('Start datetime')).toHaveValue(toDatetimeLocalValue('2026-03-05T09:00:00Z'))
    expect(screen.getByLabelText('End datetime')).toHaveValue(toDatetimeLocalValue('2026-03-05T09:27:00Z'))

    await user.click(screen.getByRole('button', { name: 'Create' }))
    expect(await screen.findByRole('heading', { name: 'sample' })).toBeInTheDocument()
  })

  it('shows a field error for an unparseable flight file', async () => {
    installFakeBackend()
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)

    await user.click(screen.getByRole('button', { name: 'Create/import' }))
    await screen.findByRole('heading', { name: 'Create activity' })

    const file = new File(['not a track file'], 'broken.gpx', { type: 'application/gpx+xml' })
    await user.upload(screen.getByLabelText('Flight file (GPX or IGC)'), file)

    expect(await screen.findByText(/could not parse/i)).toBeInTheDocument()
  })

  it('uploads the track after create (two-step flow) and shows it on the detail view', async () => {
    installFakeBackend()
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)

    await user.click(screen.getByRole('button', { name: 'Create/import' }))
    await screen.findByRole('heading', { name: 'Create activity' })

    const file = new File([sampleGpx], 'sample.gpx', { type: 'application/gpx+xml' })
    await user.upload(screen.getByLabelText('Flight file (GPX or IGC)'), file)
    await screen.findByText(/parsed 10 points/i)

    await user.click(screen.getByRole('button', { name: 'Create' }))

    expect(await screen.findByText('Track: track.gpx')).toBeInTheDocument()
  })

  it('offers a retry to upload a track on an activity that has none', async () => {
    installFakeBackend()
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)

    await user.click(screen.getByRole('button', { name: 'Create/import' }))
    await user.type(screen.getByLabelText('Name'), 'No track yet')
    await user.type(screen.getByLabelText('Start datetime'), '2026-03-05T14:30')
    await user.click(screen.getByRole('button', { name: 'Create' }))

    expect(await screen.findByText(/no track uploaded/i)).toBeInTheDocument()

    const file = new File([sampleGpx], 'retry.gpx', { type: 'application/gpx+xml' })
    await user.upload(screen.getByLabelText('Upload track'), file)

    expect(await screen.findByText('Track: track.gpx')).toBeInTheDocument()
  })
})
