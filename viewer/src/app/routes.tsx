import { ActivityList } from '../features/activities/components/ActivityList'
import { EquipmentList } from '../features/equipment/components/EquipmentList'
import { UserInfoView } from '../features/settings/components/UserInfoView'
import { UserSettingsView } from '../features/settings/components/UserSettingsView'
import type { PanelView } from '../lib/panel/types'

export const equipmentListView: PanelView = {
  key: 'equipment-list',
  title: 'Equipment',
  render: () => <EquipmentList />,
}

export const userInfoView: PanelView = {
  key: 'user-info',
  title: 'User Information',
  render: () => <UserInfoView />,
}

export const settingsRootView: PanelView = {
  key: 'settings',
  title: 'User Settings',
  render: (nav) => (
    <UserSettingsView nav={nav} userInfoView={userInfoView} equipmentListView={equipmentListView} />
  ),
}

export const activityListRootView: PanelView = {
  key: 'activities',
  title: 'Activities',
  render: (nav) => <ActivityList nav={nav} />,
}
