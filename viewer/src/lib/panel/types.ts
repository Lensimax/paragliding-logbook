import type { ReactNode } from 'react'

export interface PanelNav {
  push: (view: PanelView) => void
  pop: () => void
}

export interface PanelView {
  key: string
  title: string
  render: (nav: PanelNav) => ReactNode
  renderRight?: (nav: PanelNav) => ReactNode
}
