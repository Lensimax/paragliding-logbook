import { useCallback, useState } from 'react'
import { loadPanelWidth, savePanelWidth } from '../../lib/storage/panelWidth'

const MIN_WIDTH = 320
const DEFAULT_RATIO = 0.15
const MAX_RATIO = 0.4

function clamp(width: number): number {
  const max = Math.max(window.innerWidth * MAX_RATIO, MIN_WIDTH)
  return Math.min(Math.max(width, MIN_WIDTH), max)
}

export function usePanelWidth() {
  const [width, setWidthState] = useState(() =>
    clamp(loadPanelWidth() ?? window.innerWidth * DEFAULT_RATIO),
  )

  const setWidth = useCallback((next: number) => {
    const clamped = clamp(next)
    setWidthState(clamped)
    savePanelWidth(clamped)
  }, [])

  return { width, setWidth }
}
