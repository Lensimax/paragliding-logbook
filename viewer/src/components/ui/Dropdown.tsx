import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Button } from './Button'
import './ui.css'

interface DropdownProps {
  label: string
  icon?: ReactNode
  children: (close: () => void) => ReactNode
}

export function Dropdown({ label, icon = '⚙', children }: DropdownProps) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    function onClickOutside(event: MouseEvent) {
      if (ref.current && !ref.current.contains(event.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [open])

  return (
    <div className="ui-dropdown" ref={ref}>
      <Button
        variant="icon"
        aria-label={label}
        aria-haspopup="true"
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
      >
        {icon}
      </Button>
      {open && <div className="ui-dropdown-menu">{children(() => setOpen(false))}</div>}
    </div>
  )
}
