import type { ReactNode } from 'react'
import { Button } from '../ui/Button'

interface PanelHeaderProps {
  title: string
  onBack?: () => void
  right?: ReactNode
}

export function PanelHeader({ title, onBack, right }: PanelHeaderProps) {
  return (
    <header className="panel-header">
      {onBack ? (
        <Button variant="icon" aria-label="Back" onClick={onBack}>
          ←
        </Button>
      ) : (
        <span className="panel-header-spacer" />
      )}
      <h2 className="panel-header-title">{title}</h2>
      <span className="panel-header-right">{right}</span>
    </header>
  )
}
