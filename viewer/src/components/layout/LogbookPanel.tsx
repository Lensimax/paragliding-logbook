import type { ReactNode } from 'react'
import type { PanelStack } from '../../lib/panel/usePanelStack'
import { PanelHeader } from './PanelHeader'

interface LogbookPanelProps {
  stack: PanelStack
  /** Overrides the header for the root view, per the interface layout: username + user icon. */
  rootHeader?: { title: string; right?: ReactNode }
}

export function LogbookPanel({ stack, rootHeader }: LogbookPanelProps) {
  const { current, depth, push, pop } = stack
  const nav = { push, pop }
  const atRoot = depth === 1

  return (
    <div className="logbook-panel">
      <PanelHeader
        title={atRoot && rootHeader ? rootHeader.title : current.title}
        onBack={atRoot ? undefined : pop}
        right={atRoot ? rootHeader?.right : current.renderRight?.(nav)}
      />
      <div className="logbook-panel-body">{current.render(nav)}</div>
    </div>
  )
}
