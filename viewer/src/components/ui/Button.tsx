import type { ButtonHTMLAttributes } from 'react'
import './ui.css'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'icon'
}

export function Button({ variant = 'secondary', className, ...props }: ButtonProps) {
  const classes = ['ui-button', `ui-button-${variant}`, className].filter(Boolean).join(' ')
  return <button type="button" className={classes} {...props} />
}
