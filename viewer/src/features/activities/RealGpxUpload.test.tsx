import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../../App'
import volBelvedere from '../../lib/tracks/fixtures/vol-belvedere-maxime.gpx?raw'

const currentUser = { id: 'user-1', publicId: 'bob-ab12x', username: 'bob', email: 'bob@example.com' }

function jsonResponse(body: unknown, status = 200) {
  return { ok: status < 400, status, json: async () => body }
}

interface StoredActivity {
  id: string
  type: string
  name: string
  startedAt: string
  endedAt: string | null
  localDate: string
  track: { filename: string; format: 'gpx' | 'igc'; sizeBytes: number; sha256: string; content: string } | null
  equipmentIds: string[]
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
      if (url.includes('/api/equipment')) return jsonResponse([])

      const trackMatch = /\/api\/activities\/([^/?]+)\/track$/.exec(url)
      const idMatch = !trackMatch && /\/api\/activities\/([^/?]+)/.exec(url)

      if (trackMatch) {
        const activity = activities.get(trackMatch[1])
        if (method === 'POST' && activity) {
          const form = init?.body as FormData
          const file = form.get('file') as File
          const content = await file.text()
          activity.track = { filename: 'track.gpx', format: 'gpx', sizeBytes: content.length, sha256: 'fake-hash', content }
          const { track, ...rest } = activity
          return jsonResponse({ ...rest, track: { filename: track.filename, format: track.format, sizeBytes: track.sizeBytes, sha256: track.sha256 } })
        }
        if (method === 'GET' && activity?.track) {
          return { ok: true, status: 200, text: async () => activity.track!.content, headers: new Headers({ 'Content-Type': 'application/gpx+xml' }) }
        }
        return jsonResponse({}, 404)
      }

      if (idMatch && method === 'GET') {
        const activity = activities.get(idMatch[1])
        if (!activity) return jsonResponse({}, 404)
        const { track, ...rest } = activity
        return jsonResponse({ ...rest, track: track ? { filename: track.filename, format: track.format, sizeBytes: track.sizeBytes, sha256: track.sha256 } : null })
      }

      if (!idMatch && !trackMatch && url.includes('/api/activities') && method === 'POST') {
        const body = JSON.parse(init?.body as string)
        const id = `activity-${nextId++}`
        activities.set(id, {
          id,
          type: body.type,
          name: body.name,
          startedAt: body.startedAt,
          endedAt: body.endedAt ?? null,
          localDate: body.localDate,
          track: null,
          equipmentIds: body.equipmentIds ?? [],
        })
        return jsonResponse(
          { id, type: body.type, name: body.name, startedAt: body.startedAt, endedAt: body.endedAt ?? null, track: null, equipmentIds: body.equipmentIds ?? [] },
          201,
        )
      }

      if (!idMatch && !trackMatch && url.includes('/api/activities') && method === 'GET') {
        const items = [...activities.values()].map((a) => ({ id: a.id, type: a.type, name: a.name, startedAt: a.startedAt, endedAt: a.endedAt }))
        return jsonResponse({ items, nextCursor: null })
      }

      return jsonResponse({}, 404)
    }),
  )

  return activities
}

describe('uploading a real-world GPX export with no timestamps', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('auto-fills the name and shows a note that start/end need to be set manually', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = typeof input === 'string' ? input : input.toString()
        if (url.includes('/api/me')) return jsonResponse(currentUser)
        if (url.includes('/api/activities')) return jsonResponse({ items: [], nextCursor: null })
        if (url.includes('/api/equipment')) return jsonResponse([])
        return jsonResponse({}, 404)
      }),
    )

    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)
    await user.click(screen.getByRole('button', { name: 'Create/import' }))
    await screen.findByRole('heading', { name: 'Create activity' })

    const file = new File([volBelvedere], 'vol-belvedere-maxime.gpx', { type: 'application/gpx+xml' })
    await user.upload(screen.getByLabelText('Flight file (GPX or IGC)'), file)

    expect(await screen.findByText(/no timestamps in file/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Name')).toHaveValue('vol-belvedere-maxime')
    // Start/end are left for the user to fill in, since the file has no <time>.
    expect(screen.getByLabelText('Start datetime')).toHaveValue('')
    expect(screen.getByLabelText('End datetime')).toHaveValue('')
  })

  it('still renders the map and a distance-based altitude profile for a track with no timestamps', async () => {
    installFakeBackend()
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)
    await user.click(screen.getByRole('button', { name: 'Create/import' }))
    await screen.findByRole('heading', { name: 'Create activity' })

    const file = new File([volBelvedere], 'vol-belvedere-maxime.gpx', { type: 'application/gpx+xml' })
    await user.upload(screen.getByLabelText('Flight file (GPX or IGC)'), file)
    await screen.findByText(/no timestamps in file/i)

    // The file has no <time>, so the form doesn't auto-fill a start datetime - required to submit.
    await user.type(screen.getByLabelText('Start datetime'), '2026-03-05T14:30')
    await user.click(screen.getByRole('button', { name: 'Create' }))

    expect(await screen.findByRole('heading', { name: 'vol-belvedere-maxime' })).toBeInTheDocument()

    // Map only needs lat/lon, so it renders even without timestamps to drive the shared cursor.
    await waitFor(() => expect(document.querySelector('.flight-map.leaflet-container')).not.toBeNull())
    // The profile falls back to a distance-based x-axis instead of an empty state.
    await waitFor(() => expect(document.querySelector('.altitude-profile-chart canvas')).not.toBeNull())
    expect(document.querySelector('.altitude-profile-empty')).toBeNull()
  })
})
