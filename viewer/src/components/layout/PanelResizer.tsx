import { useCallback, useRef } from 'react'

interface PanelResizerProps {
  width: number
  onResize: (width: number) => void
}

export function PanelResizer({ width, onResize }: PanelResizerProps) {
  const startRef = useRef({ pointerX: 0, width: 0 })

  const handlePointerDown = useCallback(
    (event: React.PointerEvent<HTMLDivElement>) => {
      startRef.current = { pointerX: event.clientX, width }
      event.currentTarget.setPointerCapture(event.pointerId)
    },
    [width],
  )

  const handlePointerMove = useCallback((event: React.PointerEvent<HTMLDivElement>) => {
    if (event.buttons === 0) return
    const delta = startRef.current.pointerX - event.clientX
    onResize(startRef.current.width + delta)
  }, [onResize])

  return (
    <div
      className="panel-resizer"
      role="separator"
      aria-orientation="vertical"
      aria-label="Resize logbook panel"
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
    />
  )
}
