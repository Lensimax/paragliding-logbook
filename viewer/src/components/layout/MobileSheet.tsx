import { useCallback, useRef, useState, type ReactNode } from 'react'

type SheetState = 'peek' | 'partial' | 'full'

const PEEK_HEIGHT = 64
const PARTIAL_RATIO = 0.4

function heightFor(state: SheetState): number {
  if (state === 'peek') return PEEK_HEIGHT
  if (state === 'partial') return window.innerHeight * PARTIAL_RATIO
  return window.innerHeight
}

function nearestState(height: number): SheetState {
  const candidates: [SheetState, number][] = [
    ['peek', heightFor('peek')],
    ['partial', heightFor('partial')],
    ['full', heightFor('full')],
  ]
  return candidates.reduce((closest, candidate) =>
    Math.abs(candidate[1] - height) < Math.abs(closest[1] - height) ? candidate : closest,
  )[0]
}

interface MobileSheetProps {
  /** True once the stack has navigated past the root list, e.g. into a detail view. */
  collapsed: boolean
  children: ReactNode
}

export function MobileSheet({ collapsed, children }: MobileSheetProps) {
  const [prevCollapsed, setPrevCollapsed] = useState(collapsed)
  const [state, setState] = useState<SheetState>(collapsed ? 'partial' : 'full')
  const [dragHeight, setDragHeight] = useState<number | null>(null)
  const dragStart = useRef({ pointerY: 0, height: 0 })

  // Reset the sheet state when navigation collapses/expands it, without an extra render pass.
  if (collapsed !== prevCollapsed) {
    setPrevCollapsed(collapsed)
    setState(collapsed ? 'partial' : 'full')
    setDragHeight(null)
  }

  const handlePointerDown = useCallback((event: React.PointerEvent<HTMLDivElement>) => {
    dragStart.current = { pointerY: event.clientY, height: heightFor(state) }
    event.currentTarget.setPointerCapture(event.pointerId)
  }, [state])

  const handlePointerMove = useCallback((event: React.PointerEvent<HTMLDivElement>) => {
    if (event.buttons === 0) return
    const delta = dragStart.current.pointerY - event.clientY
    const next = Math.min(window.innerHeight, Math.max(PEEK_HEIGHT, dragStart.current.height + delta))
    setDragHeight(next)
  }, [])

  const handlePointerUp = useCallback(() => {
    if (dragHeight !== null) {
      setState(nearestState(dragHeight))
      setDragHeight(null)
    }
  }, [dragHeight])

  const height = dragHeight ?? heightFor(state)

  return (
    <div className="mobile-sheet" style={{ height }}>
      <div
        className="mobile-sheet-handle"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
      >
        <span className="mobile-sheet-grip" />
      </div>
      <div className="mobile-sheet-content">{children}</div>
    </div>
  )
}
