import { useCallback, useEffect, useState } from 'react'
import { authApi } from './api'
import type { CurrentUser, LoginPayload, RegisterPayload } from './types'

export function useAuth() {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    authApi
      .me()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false))
  }, [])

  const register = useCallback(async (payload: RegisterPayload) => {
    const registered = await authApi.register(payload)
    setUser({ id: registered.id, username: registered.username })
  }, [])

  const login = useCallback(async (payload: LoginPayload) => {
    const loggedIn = await authApi.login(payload)
    setUser({ id: loggedIn.id, username: loggedIn.username })
  }, [])

  const logout = useCallback(async () => {
    await authApi.logout()
    setUser(null)
  }, [])

  return { user, loading, register, login, logout }
}
