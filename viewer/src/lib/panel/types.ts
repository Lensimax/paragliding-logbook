import type { ReactNode } from 'react'

export interface PanelNav {
  push: (view: PanelView) => void
  pop: () => void
  /** Swaps the current top-of-stack view in place, e.g. create form -> the new item's detail view. */
  replace: (view: PanelView) => void
}

export interface PanelView {
  key: string
  title: string
  render: (nav: PanelNav) => ReactNode
  renderRight?: (nav: PanelNav) => ReactNode
}
