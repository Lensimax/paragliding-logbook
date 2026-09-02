import { useCallback, useEffect, useState } from 'react'
import type { PanelView } from './types'

export interface PanelStack {
  current: PanelView
  depth: number
  push: (view: PanelView) => void
  pop: () => void
  replace: (view: PanelView) => void
}

/**
 * A navigation stack backed by browser history, so the back button and the in-panel
 * back arrow stay in sync instead of drifting apart (the bug a route-per-view design hits).
 */
export function usePanelStack(root: PanelView): PanelStack {
  const [stack, setStack] = useState<PanelView[]>([root])

  useEffect(() => {
    function onPopState() {
      setStack((s) => (s.length > 1 ? s.slice(0, -1) : s))
    }
    window.addEventListener('popstate', onPopState)
    return () => window.removeEventListener('popstate', onPopState)
  }, [])

  const push = useCallback((view: PanelView) => {
    window.history.pushState(null, '')
    setStack((s) => [...s, view])
  }, [])

  const pop = useCallback(() => {
    window.history.back()
  }, [])

  const replace = useCallback((view: PanelView) => {
    setStack((s) => [...s.slice(0, -1), view])
  }, [])

  return { current: stack[stack.length - 1], depth: stack.length, push, pop, replace }
}
