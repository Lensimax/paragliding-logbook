import { useAuthContext } from '../../auth/AuthContext'
import { Button } from '../../../components/ui/Button'
import type { PanelNav, PanelView } from '../../../lib/panel/types'

interface UserSettingsViewProps {
  nav: PanelNav
  userInfoView: PanelView
  equipmentListView: PanelView
}

export function UserSettingsView({ nav, userInfoView, equipmentListView }: UserSettingsViewProps) {
  const { logout } = useAuthContext()

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
      <Button onClick={() => void logout()}>Log out</Button>
    </div>
  )
}
