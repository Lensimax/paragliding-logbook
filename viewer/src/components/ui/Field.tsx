import type { ReactNode } from 'react'
import './ui.css'

interface FieldProps {
  label: string
  htmlFor: string
  error?: string
  children: ReactNode
}

export function Field({ label, htmlFor, error, children }: FieldProps) {
  return (
    <div className="ui-field">
      <label htmlFor={htmlFor}>{label}</label>
      {children}
      {error && <p className="field-error">{error}</p>}
    </div>
  )
}
