import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../../App'

const currentUser = {
  id: 'user-1',
  publicId: 'bob-ab12x',
  username: 'bob',
  email: 'bob@example.com',
}

interface StoredEquipment {
  id: string
  displayName: string
  type: string
  brand: string | null
  model: string | null
  purchaseDate: string | null
  nextRevisionDate: string | null
  autoAdd: boolean
  retired: boolean
  revisions: { id: string; revisionDate: string; comment: string | null }[]
  usage: { activityCount: number; hours: number }
}

function jsonResponse(body: unknown, status = 200) {
  return { ok: status < 400, status, json: async () => body }
}

function installFakeBackend() {
  const equipment = new Map<string, StoredEquipment>()
  let nextId = 1

  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString()
      const method = (init?.method ?? 'GET').toUpperCase()

      if (url.includes('/api/me')) return jsonResponse(currentUser)
      if (url.includes('/api/activities')) return jsonResponse({ items: [], nextCursor: null })

      if (url.includes('/api/equipment')) {
        const idMatch = /\/api\/equipment\/([^/?]+)(\/retire)?$/.exec(url)
        const isRetire = idMatch?.[2] === '/retire'

        if (idMatch && !isRetire && method === 'GET') {
          const item = equipment.get(idMatch[1])
          return item ? jsonResponse(item) : jsonResponse({}, 404)
        }

        if (idMatch && !isRetire && method === 'DELETE') {
          equipment.delete(idMatch[1])
          return { ok: true, status: 204, json: async () => undefined }
        }

        if (idMatch && isRetire && method === 'POST') {
          const item = equipment.get(idMatch[1])
          if (!item) return jsonResponse({}, 404)
          item.retired = true
          return jsonResponse(item)
        }

        if (!idMatch && method === 'POST') {
          const body = JSON.parse(init?.body as string)
          const id = `equipment-${nextId++}`
          const created: StoredEquipment = {
            id,
            displayName: body.displayName,
            type: body.type,
            brand: body.brand ?? null,
            model: body.model ?? null,
            purchaseDate: body.purchaseDate ?? null,
            nextRevisionDate: body.nextRevisionDate ?? null,
            autoAdd: body.autoAdd ?? false,
            retired: false,
            revisions: (body.revisions ?? []).map((r: { revisionDate: string; comment: string | null }, i: number) => ({
              id: `revision-${i}`,
              revisionDate: r.revisionDate,
              comment: r.comment,
            })),
            usage: { activityCount: 0, hours: 0 },
          }
          equipment.set(id, created)
          return jsonResponse(created, 201)
        }

        if (!idMatch && method === 'GET') {
          const items = [...equipment.values()].map((e) => ({
            id: e.id,
            displayName: e.displayName,
            type: e.type,
            autoAdd: e.autoAdd,
            retired: e.retired,
          }))
          return jsonResponse(items)
        }
      }

      return jsonResponse({}, 404)
    }),
  )

  return equipment
}

describe('equipment CRUD flow', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('creates equipment with a revision and lands on its detail view, then deletes it', async () => {
    installFakeBackend()
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)

    await user.click(screen.getByRole('button', { name: /open user settings/i }))
    await user.click(screen.getByRole('button', { name: 'Equipment' }))
    await screen.findByText(/no equipment yet/i)

    await user.click(screen.getByRole('button', { name: 'Create equipment' }))
    await screen.findByRole('heading', { name: 'Create equipment' })

    await user.type(screen.getByLabelText('Display name'), 'Ozone Rush 6')
    await user.click(screen.getByRole('button', { name: 'Create' }))

    expect(await screen.findByRole('heading', { name: /Ozone Rush 6/ })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /back/i }))
    expect(await screen.findByText('Ozone Rush 6')).toBeInTheDocument()

    await user.click(screen.getByText('Ozone Rush 6'))
    expect(await screen.findByRole('heading', { name: /Ozone Rush 6/ })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Equipment actions' }))
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    const dialog = await screen.findByRole('dialog', { name: 'Delete equipment' })
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(screen.getByText(/no equipment yet/i)).toBeInTheDocument(), { timeout: 3000 })
  })
})
