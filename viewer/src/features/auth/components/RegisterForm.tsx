import { useState } from 'react'
import { ApiError } from '../../../lib/api/client'
import type { RegisterPayload } from '../types'

interface RegisterFormProps {
  onRegister: (payload: RegisterPayload) => Promise<void>
  onSwitchToLogin: () => void
}

export function RegisterForm({ onRegister, onSwitchToLogin }: RegisterFormProps) {
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [passwordConfirmation, setPasswordConfirmation] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setFieldErrors({})
    setSubmitting(true)
    try {
      await onRegister({ username, email, password, passwordConfirmation })
    } catch (error) {
      if (error instanceof ApiError) {
        setFieldErrors(
          Object.keys(error.fieldErrors).length > 0 ? error.fieldErrors : { _: [error.message] },
        )
      } else {
        setFieldErrors({ _: ['Something went wrong. Please try again.'] })
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <h2>Register</h2>

      <label htmlFor="register-username">Username</label>
      <input
        id="register-username"
        value={username}
        onChange={(e) => setUsername(e.target.value)}
        required
      />
      {fieldErrors.username?.map((message) => (
        <p className="field-error" key={message}>{message}</p>
      ))}

      <label htmlFor="register-email">Email</label>
      <input
        id="register-email"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />
      {fieldErrors.email?.map((message) => (
        <p className="field-error" key={message}>{message}</p>
      ))}

      <label htmlFor="register-password">Password</label>
      <input
        id="register-password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
      />
      {fieldErrors.password?.map((message) => (
        <p className="field-error" key={message}>{message}</p>
      ))}

      <label htmlFor="register-password-confirmation">Confirm password</label>
      <input
        id="register-password-confirmation"
        type="password"
        value={passwordConfirmation}
        onChange={(e) => setPasswordConfirmation(e.target.value)}
        required
      />
      {fieldErrors.passwordConfirmation?.map((message) => (
        <p className="field-error" key={message}>{message}</p>
      ))}

      {fieldErrors._?.map((message) => (
        <p className="field-error" key={message}>{message}</p>
      ))}

      <button type="submit" disabled={submitting}>
        {submitting ? 'Creating account…' : 'Create account'}
      </button>

      <p>
        Already have an account?{' '}
        <button type="button" className="link" onClick={onSwitchToLogin}>
          Sign in
        </button>
      </p>
    </form>
  )
}
