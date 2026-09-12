import { useCallback, useRef } from 'react'

interface PanelResizerProps {
  size: number
  onResize: (size: number) => void
  orientation: 'vertical' | 'horizontal'
  label: string
}

/** Drag handle for resizing an adjacent panel. Vertical = drag left/right to resize width
 * (logbook panel); horizontal = drag up/down to resize height (altitude profile). */
export function PanelResizer({ size, onResize, orientation, label }: PanelResizerProps) {
  const startRef = useRef({ pointer: 0, size: 0 })

  const handlePointerDown = useCallback(
    (event: React.PointerEvent<HTMLDivElement>) => {
      startRef.current = { pointer: orientation === 'vertical' ? event.clientX : event.clientY, size }
      event.currentTarget.setPointerCapture(event.pointerId)
    },
    [size, orientation],
  )

  const handlePointerMove = useCallback(
    (event: React.PointerEvent<HTMLDivElement>) => {
      if (event.buttons === 0) return
      if (orientation === 'vertical') {
        const delta = startRef.current.pointer - event.clientX
        onResize(startRef.current.size + delta)
      } else {
        const delta = startRef.current.pointer - event.clientY
        onResize(startRef.current.size + delta)
      }
    },
    [onResize, orientation],
  )

  return (
    <div
      className={orientation === 'vertical' ? 'panel-resizer' : 'panel-resizer panel-resizer-horizontal'}
      role="separator"
      aria-orientation={orientation === 'vertical' ? 'vertical' : 'horizontal'}
      aria-label={label}
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
    />
  )
}
