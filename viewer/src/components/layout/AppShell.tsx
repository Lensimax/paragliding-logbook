import { useEffect, useRef } from 'react'
import { useAuthContext } from '../../features/auth/AuthContext'
import { settingsRootView } from '../../app/routes'
import { makeActivityDetailView } from '../../features/activities/components/ActivityDetail'
import { useActivities } from '../../features/activities/queries'
import { usePanelStack } from '../../lib/panel/usePanelStack'
import type { PanelView } from '../../lib/panel/types'
import { useBreakpoint } from './useBreakpoint'
import { usePanelWidth } from './usePanelWidth'
import { LogbookPanel } from './LogbookPanel'
import { MapStage } from './MapStage'
import { MobileSheet } from './MobileSheet'
import { PanelResizer } from './PanelResizer'
import { Button } from '../ui/Button'
import './layout.css'

interface AppShellProps {
  rootView: PanelView
}

const MOBILE_BREAKPOINT = 768

export function AppShell({ rootView }: AppShellProps) {
  const { user } = useAuthContext()
  const isMobile = useBreakpoint(MOBILE_BREAKPOINT)
  const { width, setWidth } = usePanelWidth()
  const stack = usePanelStack(rootView)
  const { data: activityPages } = useActivities()
  const autoSelected = useRef(false)

  useEffect(() => {
    if (autoSelected.current) return
    if (stack.depth !== 1) {
      autoSelected.current = true
      return
    }
    const mostRecent = activityPages?.pages[0]?.items[0]
    if (mostRecent) {
      autoSelected.current = true
      stack.push(makeActivityDetailView(mostRecent.id))
    }
  }, [activityPages, stack.depth, stack.push])

  if (!user) return null

  const rootHeader = {
    title: user.username,
    right: (
      <Button variant="icon" aria-label="Open user settings" onClick={() => stack.push(settingsRootView)}>
        👤
      </Button>
    ),
  }

  const panel = <LogbookPanel stack={stack} rootHeader={rootHeader} />

  if (isMobile) {
    return (
      <div className="app-shell app-shell-mobile">
        <MapStage />
        <MobileSheet collapsed={stack.depth > 1}>{panel}</MobileSheet>
      </div>
    )
  }

  return (
    <div className="app-shell app-shell-desktop">
      <MapStage />
      <div className="logbook-panel-wrapper" style={{ width }}>
        <PanelResizer width={width} onResize={setWidth} />
        {panel}
      </div>
    </div>
  )
}
