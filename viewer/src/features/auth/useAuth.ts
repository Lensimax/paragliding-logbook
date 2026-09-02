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
    setUser(await authApi.register(payload))
  }, [])

  const login = useCallback(async (payload: LoginPayload) => {
    setUser(await authApi.login(payload))
  }, [])

  const logout = useCallback(async () => {
    await authApi.logout()
    setUser(null)
  }, [])

  return { user, loading, register, login, logout }
}
