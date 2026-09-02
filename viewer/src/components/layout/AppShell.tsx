import { useAuthContext } from '../../features/auth/AuthContext'
import { settingsRootView } from '../../app/routes'
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
