import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'

const currentUser = {
  id: 'user-1',
  publicId: 'bob-ab12x',
  username: 'bob',
  email: 'bob@example.com',
}

const emptyActivityList = { items: [], nextCursor: null }

function jsonResponse(body: unknown, status = 200) {
  return { ok: status < 400, status, json: async () => body }
}

function mockFetch(loggedIn: boolean) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => {
      const url = typeof input === 'string' ? input : input.toString()

      if (url.includes('/api/me')) {
        return loggedIn ? jsonResponse(currentUser) : jsonResponse({}, 401)
      }
      if (url.includes('/api/activities')) {
        return jsonResponse(emptyActivityList)
      }
      return jsonResponse({}, 404)
    }),
  )
}

describe('App', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the sign-in form when logged out', async () => {
    mockFetch(false)
    render(<App />)
    expect(await screen.findByRole('heading', { name: /sign in/i })).toBeInTheDocument()
  })

  it('shows the app shell with an empty activity list when logged in', async () => {
    mockFetch(true)
    render(<App />)
    expect(await screen.findByText(/no activities yet/i)).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'bob' })).toBeInTheDocument()
  })

  it('navigates from the activity list into settings and back', async () => {
    mockFetch(true)
    const user = userEvent.setup()
    render(<App />)

    await screen.findByText(/no activities yet/i)

    await user.click(screen.getByRole('button', { name: /open user settings/i }))
    expect(await screen.findByRole('heading', { name: 'User Settings' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Equipment' }))
    expect(await screen.findByText(/no equipment yet/i)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /back/i }))
    expect(await screen.findByRole('heading', { name: 'User Settings' })).toBeInTheDocument()
  })
})
