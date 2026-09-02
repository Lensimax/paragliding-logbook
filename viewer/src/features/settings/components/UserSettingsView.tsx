import { useState } from 'react'
import { useAuthContext } from '../../auth/AuthContext'
import { Button } from '../../../components/ui/Button'
import { downloadExport } from '../exportApi'
import type { PanelNav, PanelView } from '../../../lib/panel/types'

interface UserSettingsViewProps {
  nav: PanelNav
  userInfoView: PanelView
  equipmentListView: PanelView
}

export function UserSettingsView({ nav, userInfoView, equipmentListView }: UserSettingsViewProps) {
  const { logout } = useAuthContext()
  const [exportError, setExportError] = useState<string | null>(null)
  const [isExporting, setIsExporting] = useState(false)

  const handleExport = async () => {
    setExportError(null)
    setIsExporting(true)
    try {
      await downloadExport()
    } catch {
      setExportError('Could not download the export. Please try again.')
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <div className="user-settings">
      <ul className="nav-list">
        <li>
          <button type="button" className="nav-list-item" onClick={() => nav.push(userInfoView)}>
            User Information
          </button>
        </li>
        <li>
          <button type="button" className="nav-list-item" onClick={() => nav.push(equipmentListView)}>
            Equipment
          </button>
        </li>
      </ul>
      <Button onClick={() => void handleExport()} disabled={isExporting}>
        {isExporting ? 'Exporting…' : 'Export logbook (.zip)'}
      </Button>
      {exportError && <p className="field-error">{exportError}</p>}
      <Button onClick={() => void logout()}>Log out</Button>
    </div>
  )
}
