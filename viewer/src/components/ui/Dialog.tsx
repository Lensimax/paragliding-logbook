import { useEffect, type ReactNode } from 'react'
import './ui.css'

interface DialogProps {
  title: string
  children: ReactNode
  onClose: () => void
}

export function Dialog({ title, children, onClose }: DialogProps) {
  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  return (
    <div className="ui-dialog-backdrop" onClick={onClose}>
      <div
        className="ui-dialog"
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onClick={(event) => event.stopPropagation()}
      >
        <h3 className="ui-dialog-title">{title}</h3>
        {children}
      </div>
    </div>
  )
}
