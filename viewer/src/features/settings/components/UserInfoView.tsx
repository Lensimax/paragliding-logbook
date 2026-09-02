import { useAuthContext } from '../../auth/AuthContext'

export function UserInfoView() {
  const { user } = useAuthContext()

  if (!user) return null

  return (
    <dl className="user-info">
      <dt>Username</dt>
      <dd>{user.username}</dd>
      <dt>Email</dt>
      <dd>{user.email}</dd>
    </dl>
  )
}
