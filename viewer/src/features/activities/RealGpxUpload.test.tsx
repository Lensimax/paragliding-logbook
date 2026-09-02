import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../../App'
import volBelvedere from '../../lib/tracks/fixtures/vol-belvedere-maxime.gpx?raw'

const currentUser = { id: 'user-1', publicId: 'bob-ab12x', username: 'bob', email: 'bob@example.com' }

function jsonResponse(body: unknown, status = 200) {
  return { ok: status < 400, status, json: async () => body }
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
})
