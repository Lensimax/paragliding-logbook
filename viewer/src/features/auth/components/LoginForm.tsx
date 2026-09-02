import { useState } from 'react'
import { ApiError } from '../../../lib/api/client'
import type { LoginPayload } from '../types'

interface LoginFormProps {
  onLogin: (payload: LoginPayload) => Promise<void>
  onSwitchToRegister: () => void
}

export function LoginForm({ onLogin, onSwitchToRegister }: LoginFormProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [stayConnected, setStayConnected] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await onLogin({ email, password, stayConnected })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <h2>Sign in</h2>

      <label htmlFor="login-email">Email</label>
      <input
        id="login-email"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />

      <label htmlFor="login-password">Password</label>
      <input
        id="login-password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
      />

      <label>
        <input
          type="checkbox"
          checked={stayConnected}
          onChange={(e) => setStayConnected(e.target.checked)}
        />
        Stay connected
      </label>

      {error && <p className="field-error">{error}</p>}

      <button type="submit" disabled={submitting}>
        {submitting ? 'Connecting…' : 'Connect'}
      </button>

      <p>
        No account yet?{' '}
        <button type="button" className="link" onClick={onSwitchToRegister}>
          Register
        </button>
      </p>
    </form>
  )
}
